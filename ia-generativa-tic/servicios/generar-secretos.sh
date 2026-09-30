#!/usr/bin/env bash
# Genera los secretos locales (no se suben a git: ver .gitignore).
# Es idempotente: no sobrescribe lo que ya exista.
set -euo pipefail
cd "$(dirname "$0")"

mkdir -p secrets/tls
chmod 700 secrets

aleatoria() { openssl rand -base64 32 | tr -d '/+=' | cut -c1-32; }

for s in db_root_password db_app_password; do
  if [ ! -f "secrets/$s" ]; then
    aleatoria > "secrets/$s"
    echo "creado secrets/$s"
  fi
done

if [ ! -f secrets/tls/web.crt ]; then
  openssl req -x509 -newkey ec -pkeyopt ec_paramgen_curve:prime256v1 -nodes \
    -days 365 -subj "/CN=web.empresa.internal" \
    -addext "subjectAltName=DNS:web.empresa.internal,DNS:www.empresa.internal,DNS:intranet.empresa.internal" \
    -keyout secrets/tls/web.key -out secrets/tls/web.crt 2>/dev/null
  echo "creado certificado autofirmado secrets/tls/web.crt"
fi

if [ ! -f secrets/sftp_ed25519 ]; then
  ssh-keygen -q -t ed25519 -N '' -C "intercambio@empresa.internal" -f secrets/sftp_ed25519
  cp secrets/sftp_ed25519.pub secrets/sftp_authorized_keys
  echo "creada clave SSH secrets/sftp_ed25519"
fi

# Los contenedores leen los secretos con usuarios no root (p. ej. mysql, uid 999),
# por eso los ficheros montados son legibles; la protección en el anfitrión la da
# el directorio secrets/ con permisos 700. La clave privada SSH es solo del cliente.
chmod 644 secrets/db_root_password secrets/db_app_password secrets/tls/web.crt \
          secrets/tls/web.key secrets/sftp_authorized_keys
chmod 600 secrets/sftp_ed25519
