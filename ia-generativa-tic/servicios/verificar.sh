#!/usr/bin/env bash
# shellcheck disable=SC2015  # pasa/falla siempre devuelven 0
# Levanta la infraestructura y comprueba, extremo a extremo, que cada servicio
# funciona y que sus controles de seguridad están activos.
# Uso: ./verificar.sh            (deja los servicios arrancados)
#      ./verificar.sh --limpiar  (los para y borra volúmenes al terminar)
set -uo pipefail
cd "$(dirname "$0")" || exit 1

ok=0; fallos=0
pasa()  { printf '  \e[32m[OK]\e[0m    %s\n' "$1"; ok=$((ok+1)); }
falla() { printf '  \e[31m[FALLO]\e[0m %s\n' "$1"; fallos=$((fallos+1)); }
comprobar() { # comprobar "descripción" comando...
  local desc=$1; shift
  if "$@" >/dev/null 2>&1; then pasa "$desc"; else falla "$desc"; fi
}
rechaza() { # rechaza "descripción" comando...  (OK si el comando FALLA)
  local desc=$1; shift
  if "$@" >/dev/null 2>&1; then falla "$desc"; else pasa "$desc"; fi
}
cliente() { docker compose --progress quiet --profile pruebas run --rm -T cliente "$@"; }
# Se pregunta directamente a BIND (10.10.0.53), no al DNS interno de Docker.
dns() { cliente dig @10.10.0.53 +short "$@"; }

./generar-secretos.sh

echo "== Construyendo y arrancando (espera a los healthchecks)"
docker compose --profile pruebas build -q
if ! docker compose up -d --wait; then
  docker compose ps; docker compose logs --tail 30
  echo "No se pudieron arrancar los servicios"; exit 1
fi

echo "== Validación de configuración"
comprobar "BIND9: named-checkconf + zonas"  docker compose exec -T dns named-checkconf -z /etc/bind/named.conf
comprobar "Kea: kea-dhcp4 -t"               docker compose exec -T dhcp kea-dhcp4 -t /etc/kea/kea-dhcp4.conf
comprobar "Nginx: nginx -t"                 docker compose exec -T web nginx -t
comprobar "OpenSSH: sshd -t"                docker compose exec -T sftp sshd -t

echo "== DNS"
r=$(dns web.empresa.internal)
[ "$r" = "10.10.0.80" ] && pasa "A web.empresa.internal -> $r" || falla "A web.empresa.internal (obtenido: '$r')"
r=$(dns -x 10.10.0.33)
[ "$r" = "db.empresa.internal." ] && pasa "PTR 10.10.0.33 -> $r" || falla "PTR 10.10.0.33 (obtenido: '$r')"
r=$(dns intranet.empresa.internal | tail -1)
[ "$r" = "10.10.0.80" ] && pasa "CNAME intranet -> web" || falla "CNAME intranet (obtenido: '$r')"
r=$(dns version.bind chaos txt)
[[ "$r" != *"9."* ]] && pasa "versión de BIND oculta ($r)" || falla "BIND revela su versión: $r"
r=$(cliente dig axfr empresa.internal @10.10.0.53)
[[ "$r" == *"Transfer failed"* ]] && pasa "transferencia de zona (AXFR) denegada" || falla "AXFR permitida"

echo "== DHCP"
r=$(docker compose --progress quiet --profile pruebas run --rm -T cliente-dhcp 2>&1)
if [[ "$r" =~ lease\ of\ (10\.10\.0\.[0-9]+)\ obtained ]]; then
  ip=${BASH_REMATCH[1]}; n=${ip##*.}
  (( n >= 100 && n <= 199 )) && pasa "concesión $ip dentro del pool .100-.199" || falla "IP $ip fuera del pool"
else
  falla "sin concesión DHCP: $(echo "$r" | tail -2 | tr '\n' ' ')"
fi

echo "== Web"
comprobar "HTTPS con certificado válido para web.empresa.internal" \
  cliente curl -sf --cacert /run/secrets/tls_cert https://web.empresa.internal/salud
r=$(cliente curl -s -o /dev/null -w '%{http_code} %{redirect_url}' http://web.empresa.internal/)
[ "$r" = "301 https://web.empresa.internal/" ] && pasa "HTTP redirige a HTTPS" || falla "redirección HTTP (obtenido: '$r')"
cab=$(cliente curl -sI --cacert /run/secrets/tls_cert https://web.empresa.internal/)
for h in Strict-Transport-Security X-Content-Type-Options Content-Security-Policy Referrer-Policy; do
  grep -qi "^$h:" <<<"$cab" && pasa "cabecera $h" || falla "falta cabecera $h"
done
grep -qiE '^server: nginx/[0-9]' <<<"$cab" && falla "Nginx revela su versión" || pasa "versión de Nginx oculta"
r=$(cliente curl -s -o /dev/null -w '%{http_code}' --cacert /run/secrets/tls_cert https://web.empresa.internal/.env)
[ "$r" = "403" ] && pasa "ficheros ocultos bloqueados (/.env -> 403)" || falla "/.env devuelve $r"
rechaza "TLS 1.1 rechazado" cliente curl -sk --tls-max 1.1 https://web.empresa.internal/

echo "== Base de datos"
sql() { cliente sh -c "mariadb -h db.empresa.internal -u app -p\"\$(cat /run/secrets/db_app_password)\" inventario -N -e \"$1\""; }
comprobar "usuario app: INSERT" sql "INSERT INTO equipos (nombre,tipo,ip) VALUES ('prueba-$RANDOM','pc','10.10.0.150')"
r=$(sql "SELECT COUNT(*) FROM equipos")
[[ "$r" =~ ^[0-9]+$ ]] && (( r >= 4 )) && pasa "usuario app: SELECT ($r filas)" || falla "SELECT (obtenido: '$r')"
rechaza "usuario app NO puede borrar tablas (mínimo privilegio)" sql "DROP TABLE equipos"
rechaza "root NO accesible desde la red" cliente mariadb -h db.empresa.internal -u root -pcualquiera -e "SELECT 1"
r=$(docker compose ps db --format '{{.Ports}}')
[[ "$r" != *"->"* ]] && pasa "puerto 3306 no publicado en el anfitrión" || falla "MariaDB publicada: $r"

echo "== SFTP"
ssh_opts="-o BatchMode=yes -o StrictHostKeyChecking=accept-new -o UserKnownHostsFile=/tmp/kh"
r=$(cliente sh -c "echo hola > /tmp/f.txt && printf 'put /tmp/f.txt subidas/\nls subidas\n' | sftp $ssh_opts -b - intercambio@sftp.empresa.internal")
[[ "$r" == *"subidas/f.txt"* ]] && pasa "subida de fichero por SFTP con clave" || falla "SFTP: $r"
r=$(cliente sh -c "ssh $ssh_opts intercambio@sftp.empresa.internal 'id' 2>&1" || true)
[[ "$r" != *"uid="* ]] && pasa "shell interactiva denegada (solo SFTP)" || falla "se obtuvo shell: $r"
rechaza "autenticación sin clave rechazada" cliente ssh -o BatchMode=yes -o PubkeyAuthentication=no \
  -o StrictHostKeyChecking=no intercambio@sftp.empresa.internal true
r=$(cliente sh -c "printf 'cd /etc\nls\n' | sftp $ssh_opts -b - intercambio@sftp.empresa.internal 2>&1" || true)
[[ "$r" != *"passwd"* ]] && pasa "usuario enjaulado (chroot): no ve /etc" || falla "chroot roto: ve /etc"

echo
echo "Resultado: $ok correctas, $fallos fallidas"
if [ "${1:-}" = "--limpiar" ]; then docker compose --profile pruebas down -v; fi
[ "$fallos" -eq 0 ]
