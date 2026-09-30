# Uso de la IA generativa en entornos TIC

**Informe técnico y crítico. Caso práctico: despliegue de servicios de red de una pyme con ayuda de IA**

| | |
|---|---|
| Fecha | 30 de septiembre de 2026 |
| Código del caso práctico | [`servicios/`](servicios/) (Docker Compose: DNS, DHCP, web, base de datos y SFTP) |
| Verificación | [`servicios/verificar.sh`](servicios/verificar.sh): 28 de 28 pruebas superadas |

> **Declaración de uso de IA.** Este trabajo se ha hecho con un asistente de IA
> generativa de tipo agente (Claude Code), que redactó el código y el texto y
> ejecutó las pruebas. Todas las configuraciones se han validado con las
> herramientas propias de cada servicio y con pruebas reales de extremo a
> extremo. Declarar el uso de IA es, en sí mismo, una de las recomendaciones del
> apartado 7.

## Índice

1. [Conceptos clave de la IA generativa](#1-conceptos-clave-de-la-ia-generativa)
2. [Riesgos y limitaciones en entornos TIC](#2-riesgos-y-limitaciones-en-entornos-tic)
3. [Generación de código funcional con IA: el caso práctico](#3-generación-de-código-funcional-con-ia-el-caso-práctico)
4. [Análisis técnico de diferencias](#4-análisis-técnico-de-diferencias)
5. [Ventajas y desventajas](#5-ventajas-y-desventajas)
6. [Informe crítico](#6-informe-crítico)
7. [Recomendaciones alineadas con la normativa](#7-recomendaciones-alineadas-con-la-normativa)
8. [Referencias](#8-referencias)
- [Anexo A. Prompts utilizados](#anexo-a-prompts-utilizados)

---

## 1. Conceptos clave de la IA generativa

La **inteligencia artificial (IA)** es la disciplina que construye sistemas
capaces de hacer tareas que asociamos a la inteligencia humana. Dentro de ella,
el **aprendizaje automático** (*machine learning*) aprende patrones a partir de
datos en lugar de programarlos a mano. El **aprendizaje profundo** (*deep
learning*) es la parte del aprendizaje automático que usa redes neuronales de
muchas capas. La **IA generativa** es la rama del aprendizaje profundo que
**crea contenido nuevo** (texto, código, imágenes, audio o vídeo) a partir de lo
que aprendió en el entrenamiento, en vez de limitarse a clasificar o predecir
una etiqueta.

```
IA  ⊃  Aprendizaje automático  ⊃  Aprendizaje profundo  ⊃  IA generativa (LLM, difusión…)
```

| Concepto | Qué es | Ejemplo relevante en TIC |
|---|---|---|
| **Modelo de lenguaje grande (LLM)** | Red neuronal entrenada con enormes cantidades de texto y código para **predecir el siguiente *token***. De esa capacidad salen la redacción, la traducción, el razonamiento y la programación. | GPT (OpenAI), Claude (Anthropic), Gemini (Google), Llama (Meta), Mistral. |
| **Arquitectura *Transformer*** | Arquitectura de red (Vaswani et al., 2017) basada en el mecanismo de **atención**, que relaciona cada *token* con todos los demás del contexto. Es la base de todos los LLM actuales. | Permite que el modelo relacione una directiva de `named.conf` con la zona definida 40 líneas más abajo. |
| ***Token*** | Unidad mínima que procesa el modelo (un fragmento de palabra, un símbolo o un espacio). Los límites y los precios se miden en *tokens*. | `ssl_protocols` puede ocupar 2 o 3 *tokens*. Un fichero de configuración grande consume contexto. |
| **Ventana de contexto** | Cantidad máxima de *tokens* (instrucción + historial + ficheros) que el modelo tiene en cuenta a la vez. Lo que queda fuera, el modelo «no lo ve». | Si el `compose.yaml` no está en el contexto, la IA puede inventarse nombres de red o IP incoherentes. |
| ***Prompt* e ingeniería de *prompts*** | La instrucción que se da al modelo y la técnica de redactarla con **contexto, restricciones, formato y ejemplos** para obtener mejores resultados. | En el [apartado 4](#4-análisis-técnico-de-diferencias) se comparan un *prompt* mínimo y uno con requisitos de seguridad. |
| **Inferencia y temperatura** | La inferencia es el uso del modelo ya entrenado. La **temperatura** regula cuánto azar hay al elegir *tokens*: si es baja, la salida es más determinista; si es alta, más creativa. | Para configuraciones interesa una temperatura baja. El mismo *prompt* puede dar respuestas distintas: **la salida no es determinista**. |
| **Alucinación** | Contenido plausible pero **falso**: directivas que no existen, opciones de otra versión o paquetes inventados. | Recomendar `isc-dhcp-server` (sin mantenimiento desde 2022) como si fuera la opción actual, o inventar una opción de Kea. |
| **Fecha de corte del conocimiento** | El modelo solo «sabe» lo que había en sus datos de entrenamiento. | Puede no conocer que Nginx 1.25.1 cambió `listen … http2` por la directiva `http2 on;`. |
| ***Fine-tuning* (ajuste fino)** | Reentrenar un modelo base con datos propios para especializarlo. | Un modelo ajustado con los procedimientos internos de un CPD. |
| **RAG (*Retrieval-Augmented Generation*)** | Antes de generar, se **recuperan documentos relevantes** (manuales, *wikis*, tickets) y se añaden al contexto. Reduce las alucinaciones y aporta información privada o reciente sin reentrenar. | Un *chatbot* de soporte que responde con la documentación interna de la red de la empresa. |
| ***Embeddings*** | Representación de un texto como un vector numérico; los textos con significado parecido quedan cerca. Es la base de la búsqueda semántica y del RAG. | Buscar incidencias parecidas en el histórico del *helpdesk*. |
| **Multimodalidad** | Modelos que entienden o generan varios tipos de dato: texto, imagen, audio o vídeo. | Subir una captura de un error de Windows o un diagrama de red y pedir el diagnóstico. |
| **Asistente de código (*copiloto*)** | LLM integrado en el editor que autocompleta y genera código en contexto. | GitHub Copilot, Gemini Code Assist, Cursor. |
| **Agente de IA** | LLM que además **usa herramientas** (terminal, ficheros, APIs) en un bucle de planificar, actuar y observar para cumplir un objetivo. | Claude Code u OpenAI Codex: editan ficheros, ejecutan `docker compose` y corrigen hasta que pasan las pruebas, como en este trabajo. |
| **MCP (*Model Context Protocol*)** | Protocolo abierto para conectar agentes con herramientas y datos externos (GitHub, bases de datos, *tickets*). | Un agente que abre una incidencia en el gestor de *tickets* tras detectar un fallo. |
| **Modelos abiertos y cerrados** | Los de **pesos abiertos** (Llama, Mistral, Qwen) se pueden ejecutar en local; los **cerrados** (GPT, Claude, Gemini) solo por API o web. | Un modelo local con Ollama evita enviar datos fuera de la empresa, a costa de capacidad y de hardware. |
| **Modelos de difusión** | Generan imágenes o vídeo eliminando ruido paso a paso. | Stable Diffusion, Midjourney o DALL·E para iconos, diagramas o material de formación. |
| **IA de propósito general (GPAI)** | Término del Reglamento Europeo de IA para modelos versátiles integrables en muchos sistemas. Tienen obligaciones propias desde el 2 de agosto de 2025. | Todos los LLM citados. Ver el [apartado 7](#7-recomendaciones-alineadas-con-la-normativa). |

**Idea clave:** un LLM no «consulta» una base de conocimiento verificada, sino
que **genera la continuación más probable**. Por eso escribe muy bien código
habitual y, al mismo tiempo, puede equivocarse con total seguridad. Todo lo que
sigue en este informe parte de esa propiedad.

---

## 2. Riesgos y limitaciones en entornos TIC

Para cada riesgo se indica **cómo se manifiesta en sistemas y redes**, **qué
impacto tiene** y **cómo se mitiga**. Varios se han **observado de verdad**
durante este trabajo (marcados con 🔎).

### 2.1 Riesgos técnicos

| # | Riesgo | Manifestación en TIC | Impacto | Mitigación |
|---|---|---|---|---|
| T1 | **Alucinaciones** | Directivas, parámetros o comandos inexistentes o de otra versión. | Servicios que no arrancan o, peor, que arrancan con un comportamiento distinto del esperado. | Validar con la propia herramienta (`named-checkconf`, `kea-dhcp4 -t`, `nginx -t`, `sshd -t`) y probar. |
| T2 | **Código inseguro por defecto** | Contraseñas en claro en el `compose`, BD publicada en `0.0.0.0:3306`, DNS con recursión abierta, FTP sin cifrar, `latest` como versión. | Brechas, amplificación DDoS, fuga de credenciales. Perry et al. (2023) comprobaron que quien programaba con asistente escribía código **menos seguro** y además **confiaba más** en él. | *Prompts* con requisitos de seguridad, revisión humana, SAST y escáneres de secretos. |
| T3 | **Conocimiento desactualizado** | Recomendar software sin mantenimiento (`isc-dhcp-server`), sintaxis obsoleta (`listen 443 ssl http2`), versiones con CVE. | Deuda técnica y vulnerabilidades conocidas. | Dar la versión objetivo en el *prompt*, contrastar con la documentación oficial y fijar versiones. |
| T4 | **Paquetes alucinados (*slopsquatting*)** | La IA recomienda instalar un paquete o una imagen que no existe; un atacante lo registra con código malicioso. Spracklen et al. (2025) encontraron que cerca del **20 %** de los paquetes sugeridos por los modelos analizados no existían. | Compromiso de la cadena de suministro. | Comprobar el origen de cada dependencia, usar repositorios o *registries* internos y generar SBOM. |
| T5 | **Errores sutiles que «parecen» correctos** 🔎 | En este trabajo, la primera versión del DNS montaba `/var/cache/bind` en un *tmpfs* sin permisos para el usuario `bind`: el contenedor entraba en bucle de reinicios. | Caídas de servicio. | Pruebas automáticas **antes** de dar nada por bueno. |
| T6 | **Pruebas que prueban lo que no es** 🔎 | La prueba PTR inicial preguntaba al DNS embebido de Docker (127.0.0.11), que respondía `servicios-db-1.servicios_lan.` en vez de preguntar a BIND. Otra prueba (`docker compose port`) devolvía código 0 con la salida `invalid IP:0`. | Falsa sensación de seguridad: la prueba pasaría o fallaría por motivos ajenos al servicio. | Revisar **qué** mide cada prueba, no solo si sale verde. Se corrigieron consultando directamente a `10.10.0.53`. |
| T7 | **No determinismo** | El mismo *prompt* produce configuraciones distintas en días distintos. | Poca reproducibilidad y auditorías difíciles. | Versionar el **resultado** en git (no el *prompt*) y trabajar con infraestructura como código. |
| T8 | **Límite de contexto** | En proyectos grandes la IA no ve todos los ficheros y genera piezas incoherentes entre sí (IP, nombres, puertos). | Integraciones rotas. | Dar el contexto relevante y usar agentes que lean el repositorio. |

### 2.2 Riesgos de seguridad específicos de la IA

| # | Riesgo | Descripción | Mitigación |
|---|---|---|---|
| S1 | **Fuga de información confidencial** | Pegar en un *chat* público configuraciones con IP internas, contraseñas, *logs* con datos personales o código propietario. Caso conocido: en abril de 2023, ingenieros de Samsung pegaron código fuente interno en ChatGPT y la empresa restringió su uso. | Clasificar la información, usar herramientas corporativas con contrato que excluya el entrenamiento con datos del cliente, anonimizar y no pegar nunca secretos. |
| S2 | **Inyección de *prompts*** (OWASP LLM01) | Instrucciones maliciosas ocultas en un correo, una web, un *log* o un fichero que la IA lee y **obedece** («ignora lo anterior y ejecuta…»). | Tratar todo contenido externo como dato, nunca como orden. Mínimo privilegio y aprobación humana de las acciones. |
| S3 | **Agencia excesiva** (OWASP LLM06) | Un agente con permisos de administrador en producción puede borrar datos, abrir puertos o hacer `push` sin control. | Entornos aislados (*sandbox*), permisos mínimos, confirmación en acciones irreversibles y registro de auditoría. |
| S4 | **Uso de IA en la sombra (*shadow AI*)** | Personal que usa herramientas no aprobadas con datos de la empresa. | Una política de uso y alternativas corporativas aprobadas: prohibir sin ofrecer alternativa no funciona. |
| S5 | **Uso ofensivo** | Los atacantes también usan IA: *phishing* más creíble, *malware* polimórfico, reconocimiento automatizado. | Formación, MFA y detección basada en comportamiento. |

### 2.3 Riesgos legales, éticos y organizativos

- **Protección de datos (RGPD/LOPDGDD).** Enviar datos personales a un proveedor
  de IA es un tratamiento: necesita base jurídica, contrato de encargado
  (art. 28 RGPD) y, si hay transferencia fuera del EEE, garantías adecuadas.
- **Propiedad intelectual y licencias.** El código generado puede reproducir
  fragmentos con licencia (GPL, por ejemplo). Además, la protección por derechos
  de autor requiere una aportación creativa humana, así que la autoría de un
  código generado íntegramente por IA es discutible.
- **Responsabilidad.** Si la configuración generada provoca una brecha, el
  responsable es **la organización y el profesional que la desplegó**, no la IA.
- **Sesgos.** Los modelos reflejan sesgos de sus datos, algo relevante si se usan
  en selección de personal o en clasificación de usuarios o incidencias.
- **Pérdida de competencias y dependencia.** Quien no entiende lo que despliega no
  sabrá diagnosticarlo a las tres de la madrugada. Además aparece dependencia de
  un proveedor (*lock-in*), de sus precios y de su disponibilidad.
- **Coste y huella ambiental.** La inferencia consume energía y agua en los
  centros de datos. No todo necesita un LLM: un `grep` o una plantilla suelen
  bastar.

### 2.4 Limitaciones intrínsecas

La IA **no ejecuta ni comprueba** nada por sí misma (salvo que un agente tenga
herramientas para ello), **no conoce tu red** (IP, VLAN, políticas), **no tiene
responsabilidad** y **no garantiza** la corrección. Es una herramienta
probabilística que **requiere supervisión humana cualificada**.

---

## 3. Generación de código funcional con IA: el caso práctico

### 3.1 Enunciado del caso

Una pyme ficticia (`empresa.internal`) necesita desplegar en su red
`10.10.0.0/24` estos servicios básicos:

| Servicio | Software | IP | Función |
|---|---|---|---|
| **DNS** | BIND 9 | 10.10.0.53 | Zona directa `empresa.internal`, zona inversa `0.10.10.in-addr.arpa` y resolución recursiva **solo para la LAN**. |
| **DHCP** | ISC Kea 2.4 | 10.10.0.67 | Pool `.100–.199`, puerta de enlace, DNS y dominio de búsqueda, y reserva por MAC para la impresora. |
| **Web** | Nginx 1.27 | 10.10.0.80 | Intranet por HTTPS con redirección de 80 a 443 y cabeceras de seguridad. |
| **Base de datos** | MariaDB 11.4 LTS | 10.10.0.33 | BD `inventario` con usuario de aplicación de **mínimo privilegio**. |
| **Transferencia de ficheros** | OpenSSH (SFTP) | 10.10.0.22 | Solo SFTP, enjaulado (*chroot*), autenticación **solo por clave**. |

Todo se orquesta con **Docker Compose** para que sea reproducible en cualquier
equipo con Docker.

### 3.2 Estructura del código

```
servicios/
├── compose.yaml            # Orquestación: red, IP fijas, secretos, healthchecks, endurecimiento
├── generar-secretos.sh     # Contraseñas aleatorias, certificado TLS y clave SSH (no se suben a git)
├── verificar.sh            # 28 pruebas extremo a extremo
├── .env.example / .gitignore
├── dns/   Dockerfile · named.conf · zones/db.empresa.internal · zones/db.10.10.0
├── dhcp/  Dockerfile · kea-dhcp4.conf
├── web/   nginx.conf · html/index.html
├── db/    init/01-esquema.sql · init/02-usuario-app.sh
├── sftp/  Dockerfile · sshd_config · entrypoint.sh
└── pruebas/Dockerfile      # Cliente con dig, curl, mariadb y ssh para las pruebas
```

### 3.3 Cómo ejecutarlo

```bash
cd servicios
./generar-secretos.sh                    # una sola vez
docker compose up -d --build --wait      # arranca y espera a que todo esté sano
./verificar.sh                           # pruebas (añade --limpiar para borrar todo al terminar)
```

Por defecto, la web (`8080`/`8443`) y el SFTP (`2222`) solo se publican en
`127.0.0.1`. Para dar servicio a la LAN, copia `.env.example` a `.env` y pon la
IP de la interfaz en `BIND_IP`. Para un DHCP real en la LAN física, el
contenedor `dhcp` debe usar `network_mode: host` o una red `macvlan`, porque la
red *bridge* de Docker no llega a los equipos físicos.

### 3.4 Buenas prácticas aplicadas

| Práctica | Dónde |
|---|---|
| **Versiones fijadas** (`nginx:1.27-alpine`, `mariadb:11.4`, `ubuntu:24.04`) en lugar de `latest` | `compose.yaml`, `Dockerfile` |
| **Validación de la configuración al construir la imagen**: si hay un error, la imagen no se crea | `RUN named-checkconf -z`, `RUN kea-dhcp4 -t` |
| **Secretos fuera del código**: *Docker secrets* y ficheros generados en local, excluidos por `.gitignore` | `generar-secretos.sh`, `secrets:` |
| **Mínimo privilegio**: `cap_drop: [ALL]` y solo las capacidades imprescindibles, `no-new-privileges`, sistema de ficheros de solo lectura donde es posible | `x-endurecido`, `read_only` |
| **Superficie mínima**: MariaDB sin puerto publicado y puertos publicados solo en `127.0.0.1` por defecto | `ports:` |
| **Healthchecks** y arranque ordenado (`depends_on: condition: service_healthy`) | todos los servicios |
| **Persistencia** en volúmenes con nombre (BD, concesiones DHCP, claves de host SSH) | `volumes:` |
| **Registros** hacia `stdout`/`stderr` con rotación (`max-size`) | `logging:` y configuración de cada servicio |
| **Comentarios que explican el porqué**, no el qué | todos los ficheros |
| **Análisis estático** de los *scripts* con `shellcheck` (sin avisos) | `*.sh` |

Medidas de seguridad por servicio:

- **DNS:** recursión y consultas solo desde la ACL `red_interna` (no es un
  resolutor abierto), AXFR denegado, versión oculta, DNSSEC validado y dominio
  `.internal`, reservado por ICANN en 2024 para uso privado.
- **DHCP:** Kea (el sucesor mantenido de `isc-dhcp-server`), pool separado de las
  IP fijas, `authoritative` y concesiones persistentes.
- **Web:** solo TLS 1.2 y 1.3, HSTS, CSP, `nosniff`, `Referrer-Policy`,
  `Permissions-Policy`, `server_tokens off`, ficheros ocultos bloqueados y
  `http2 on;` (sintaxis actual).
- **BD:** el usuario `app` solo tiene `SELECT/INSERT/UPDATE/DELETE` sobre
  `inventario` y solo desde `10.10.0.%`. `root` solo es accesible por *socket*
  local.
- **SFTP:** sin *root*, sin contraseñas, sin *shell* (`ForceCommand
  internal-sftp`), *chroot*, sin reenvíos ni túneles, `MaxAuthTries 3` y claves
  de host persistentes (evitan falsas alarmas de MITM).

### 3.5 Resultado de la verificación

`./verificar.sh --limpiar` se ejecutó desde un estado limpio (sin volúmenes
previos) con este resultado:

```
== Validación de configuración
  [OK]    BIND9: named-checkconf + zonas
  [OK]    Kea: kea-dhcp4 -t
  [OK]    Nginx: nginx -t
  [OK]    OpenSSH: sshd -t
== DNS
  [OK]    A web.empresa.internal -> 10.10.0.80
  [OK]    PTR 10.10.0.33 -> db.empresa.internal.
  [OK]    CNAME intranet -> web
  [OK]    versión de BIND oculta ("no disponible")
  [OK]    transferencia de zona (AXFR) denegada
== DHCP
  [OK]    concesión 10.10.0.100 dentro del pool .100-.199
== Web
  [OK]    HTTPS con certificado válido para web.empresa.internal
  [OK]    HTTP redirige a HTTPS
  [OK]    cabecera Strict-Transport-Security
  [OK]    cabecera X-Content-Type-Options
  [OK]    cabecera Content-Security-Policy
  [OK]    cabecera Referrer-Policy
  [OK]    versión de Nginx oculta
  [OK]    ficheros ocultos bloqueados (/.env -> 403)
  [OK]    TLS 1.1 rechazado
== Base de datos
  [OK]    usuario app: INSERT
  [OK]    usuario app: SELECT (4 filas)
  [OK]    usuario app NO puede borrar tablas (mínimo privilegio)
  [OK]    root NO accesible desde la red
  [OK]    puerto 3306 no publicado en el anfitrión
== SFTP
  [OK]    subida de fichero por SFTP con clave
  [OK]    shell interactiva denegada (solo SFTP)
  [OK]    autenticación sin clave rechazada
  [OK]    usuario enjaulado (chroot): no ve /etc
Resultado: 28 correctas, 0 fallidas
```

Las pruebas **integran** los servicios entre sí: el cliente resuelve
`web.empresa.internal` y `db.empresa.internal` a través de BIND, y el
certificado TLS se valida contra ese nombre.

**Limitación honesta de las pruebas:** la prueba «TLS 1.1 rechazado» no
distingue si lo rechaza el servidor o el propio cliente, porque OpenSSL 3 también
desactiva TLS 1.1 en el cliente por defecto. La garantía real la da la directiva
`ssl_protocols TLSv1.2 TLSv1.3` de `nginx.conf`. Tampoco se ha probado la
recursión hacia Internet (los *forwarders*), porque el entorno de pruebas no
tenía salida DNS.

---

## 4. Análisis técnico de diferencias

Se compara lo que produce la IA ante dos formas de pedir lo mismo:

- **Versión A (*prompt* mínimo):** «Hazme un docker-compose con DNS, DHCP, web,
  base de datos y FTP para una empresa».
- **Versión B (*prompt* con requisitos + iteración con pruebas):** el *prompt*
  del [Anexo A](#anexo-a-prompts-utilizados) más el ciclo de generar, validar,
  probar y corregir. Es el código de `servicios/`.

> **Sobre la versión A.** Es una **reconstrucción ilustrativa** de los patrones
> que los asistentes generan con frecuencia ante *prompts* genéricos (y que
> recogen fuentes como OWASP o Perry et al., 2023), no la salida literal de una
> herramienta concreta. Como la salida no es determinista, es recomendable
> repetir el experimento con la herramienta propia y guardar la respuesta
> literal.

Fragmento típico de la versión A:

```yaml
version: "3"
services:
  dns:
    image: ubuntu/bind9:latest
    ports: ["53:53/udp"]
  dhcp:
    image: networkboot/dhcpd          # isc-dhcp-server
    network_mode: host
  web:
    image: nginx:latest
    ports: ["80:80"]
  db:
    image: mysql:latest
    environment:
      MYSQL_ROOT_PASSWORD: admin123
    ports: ["3306:3306"]
  ftp:
    image: fauria/vsftpd
    environment:
      FTP_USER: admin
      FTP_PASS: admin
    ports: ["21:21", "21100-21110:21100-21110"]
```

### 4.1 Diferencias generales (orquestación)

| Aspecto | Versión A | Versión B | Justificación técnica |
|---|---|---|---|
| Versiones de imagen | `latest` | Fijadas (`nginx:1.27-alpine`, `mariadb:11.4`…) | `latest` cambia sin aviso: un `pull` puede traer una versión mayor incompatible. Fijar la versión da reproducibilidad y permite gestionar parches de forma controlada. |
| Clave `version:` | `version: "3"` | No se usa | La especificación de Compose la declara **obsoleta** y Compose v2 la ignora con un aviso. |
| Credenciales | En claro en el YAML (`admin123`) | *Docker secrets* generados al azar, fuera de git | Un secreto en el repositorio queda en el historial para siempre. Con los secretos en ficheros montados en `/run/secrets`, no aparecen en `docker inspect` ni en las variables de entorno. |
| Red | La red por defecto, IP dinámicas | Red `lan` con subred, IP fijas y un `ip_range` que evita choques con el pool DHCP | Los servidores de infraestructura necesitan IP estables (el DNS apunta a ellas). Si no se limita el rango dinámico de Docker, podría asignar una IP del pool de Kea. |
| Exposición | Todo publicado en `0.0.0.0` | Solo web y SFTP, en `127.0.0.1` por defecto; la BD no se publica | Principio de mínima exposición: la BD solo la usan otros contenedores de la LAN. |
| Privilegios | Los que Docker da por defecto | `cap_drop: ALL` con lista blanca, `no-new-privileges`, `read_only` | Si un servicio se ve comprometido, el atacante tiene muchas menos capacidades (montar, cambiar la red, escalar con *setuid*). |
| Salud y arranque | Sin *healthchecks* | *Healthchecks* y `depends_on: service_healthy` | Sin ellos, `up` da por arrancado un servicio que aún no acepta conexiones y las dependencias fallan de forma intermitente. |
| Verificación | Ninguna | `named-checkconf`, `kea-dhcp4 -t` al construir la imagen y 28 pruebas | «Arranca» no significa «funciona». Solo una prueba demuestra que el servicio cumple su requisito. |

### 4.2 Diferencias por servicio

**DNS**

| Versión A | Versión B | Justificación |
|---|---|---|
| Dominio `empresa.local` | `empresa.internal` | `.local` está reservado para mDNS (RFC 6762): macOS, iOS y Linux con Avahi mandan esas consultas por multidifusión y no al DNS, así que la resolución falla de forma intermitente. `.internal` fue reservado por ICANN (2024) para redes privadas. |
| `recursion yes;` sin ACL | `allow-recursion { red_interna; }` y `allow-query` | Un resolutor abierto se usa en ataques de **amplificación DDoS**, y cualquiera podría envenenar su caché. |
| Transferencia de zona sin restringir | `allow-transfer { none; }` | Un AXFR abierto entrega el mapa completo de la red (reconocimiento). |
| Versión visible | `version "no disponible";` | Revelar la versión facilita buscar CVE concretos. |
| Solo zona directa | Zona directa e **inversa** | Muchos servicios (SSH, correo, *logs*) hacen búsquedas PTR. Sin zona inversa hay retardos y *logs* sin nombres. |
| `type master` | `type primary` | Terminología actual de BIND 9.18 o superior (`master` sigue aceptándose por compatibilidad). |

**DHCP**

| Versión A | Versión B | Justificación |
|---|---|---|
| `isc-dhcp-server` (`dhcpd.conf`) | **ISC Kea** (`kea-dhcp4.conf`, JSON) | ISC dejó de mantener ISC DHCP a finales de 2022: no recibe parches de seguridad. Kea es su sucesor, con configuración validable (`-t`), API de control y base de datos de concesiones. |
| Pool que incluye IP de servidores | Pool `.100–.199` separado de las IP fijas | Si el pool se solapa con las IP fijas, se producen conflictos de IP: dos equipos responden a la misma dirección. |
| Sin reservas | Reserva por MAC (impresora → `.50`) coherente con el registro A del DNS | Los dispositivos compartidos necesitan una IP estable y conocida, y el DNS debe estar de acuerdo con el DHCP. |
| Sin `domain-search` | `domain-name` y `domain-search` | Permite escribir `ssh web` en lugar del nombre completo. |

**Web**

| Versión A | Versión B | Justificación |
|---|---|---|
| Solo HTTP en el puerto 80 | HTTPS en el 443 con redirección 301 desde el 80 | Sin TLS, las credenciales y las *cookies* viajan en claro por la LAN (ARP *spoofing*). |
| `listen 443 ssl http2;` (cuando añade TLS) | `listen 443 ssl;` + `http2 on;` | Sintaxis obsoleta desde Nginx 1.25.1: genera avisos y desaparecerá. Es un ejemplo típico de conocimiento desactualizado. |
| Sin cabeceras de seguridad | HSTS, CSP, `nosniff`, `Referrer-Policy`, `Permissions-Policy` | Mitigan XSS, *clickjacking*, rastreo del *MIME* y degradación a HTTP. |
| `server_tokens` activado | `server_tokens off` | Menos información para el reconocimiento. |
| Sin reglas para ficheros ocultos | `location ~ /\. { deny all; }` | Evita exponer `.git`, `.env` o `.htpasswd` si se copian por error al *docroot*. |

**Base de datos**

| Versión A | Versión B | Justificación |
|---|---|---|
| `mysql:latest` | `mariadb:11.4` (LTS) | Una versión LTS da soporte y parches durante años; `latest` puede traer cambios de versión mayor. |
| La aplicación usa `root` | Usuario `app` solo con CRUD sobre `inventario` y desde `10.10.0.%` | **Mínimo privilegio**: si roban la credencial de la aplicación, no pueden borrar tablas ni leer otras BD (comprobado: `DROP TABLE` falla). |
| `3306` publicado al exterior | Sin publicar | La BD no debe ser accesible desde fuera de la red de servicios. |
| Sin comprobar la inicialización | `healthcheck.sh --connect --innodb_initialized` | Asegura que InnoDB está listo antes de que otros dependan de la BD. |

**Transferencia de ficheros**

| Versión A | Versión B | Justificación |
|---|---|---|
| **FTP** (vsftpd, puertos 21 y pasivos) | **SFTP** (OpenSSH) | FTP envía el usuario y la contraseña **en claro** y necesita un rango de puertos pasivos abierto en el cortafuegos. SFTP cifra todo por un único puerto. |
| Usuario `admin`/`admin` | Solo clave pública Ed25519, `PasswordAuthentication no` | Las credenciales por defecto son la primera prueba de cualquier ataque de fuerza bruta. |
| Acceso a todo el sistema de ficheros | `ChrootDirectory` y `ForceCommand internal-sftp` | El usuario solo ve su carpeta y no puede abrir una *shell* (comprobado en las pruebas). |
| Claves de host nuevas en cada arranque | Claves de host persistentes en un volumen | Si cambian en cada reinicio, los clientes ven el aviso de «posible MITM» y se acostumbran a ignorarlo. |

### 4.3 Diferencias entre iteraciones de la propia IA (observadas)

Incluso con un buen *prompt*, **la primera versión de la versión B no funcionó
del todo**. Estas son las diferencias reales entre la primera iteración y la
versión final:

| Primera iteración | Versión final | Causa técnica |
|---|---|---|
| `tmpfs: [/var/cache/bind]` | `tmpfs: ["/var/cache/bind:mode=1777", …]` | `named` arranca como *root* y baja al usuario `bind`. Con `cap_drop: ALL`, *root* pierde `DAC_OVERRIDE` y el *tmpfs* no tenía permisos para `bind`, así que el contenedor se reiniciaba en bucle. |
| `dig +short -x 10.10.0.33` | `dig @10.10.0.53 +short -x 10.10.0.33` | El DNS embebido de Docker (127.0.0.11) responde **él mismo** a los PTR de IP de contenedores. La prueba no llegaba a BIND. |
| `docker compose port db 3306` para comprobar que no está publicado | Leer `Ports` de `docker compose ps` | Esa orden devolvía código 0 con la salida `invalid IP:0`: la prueba daba un falso fallo. |
| *Scripts* con avisos de `shellcheck` | Sin avisos | `cd` sin control de errores y construcciones `A && B \|\| C` mal documentadas. |

Estas diferencias muestran algo central del informe: **la calidad no depende
solo del modelo, sino del proceso de verificación** que se construye alrededor.

### 4.4 Diferencias entre tipos de herramienta

| | *Chat* web (ChatGPT, Claude, Gemini) | Copiloto en el IDE (Copilot) | Agente (Claude Code, Codex) |
|---|---|---|---|
| Contexto | Solo lo que se pega | El fichero abierto y los vecinos | El repositorio completo |
| Ejecuta y prueba | No | No (sugiere) | Sí, en la terminal |
| Riesgo principal | Fuga de datos al pegar, código sin probar | Aceptar sugerencias sin leerlas | Acciones con demasiados permisos |
| Uso ideal | Explicar y hacer borradores | Código repetitivo al escribir | Tareas completas con pruebas, en un entorno aislado |

---

## 5. Ventajas y desventajas

### 5.1 Ventajas

| Ventaja | Ejemplo concreto |
|---|---|
| **Productividad** | Generar la estructura de cinco servicios, *healthchecks* y *scripts* lleva minutos en lugar de horas. En un experimento controlado con GitHub Copilot (Peng et al., 2023), el grupo con asistente terminó la tarea un **55 % más rápido**. |
| **Menos barrera de entrada** | Un técnico que nunca ha configurado Kea obtiene un `kea-dhcp4.conf` comentado y válido como punto de partida, y aprende la estructura JSON en el proceso. |
| **Buenas prácticas «gratis», si se piden** | Al pedir «mínimo privilegio y secretos fuera del código», la IA propone `cap_drop`, *Docker secrets* y un usuario de BD restringido que un principiante no habría tenido en cuenta. |
| **Explicación y aprendizaje** | Explica línea a línea por qué `allow-recursion` evita un resolutor abierto. Es un tutor disponible siempre. |
| **Diagnóstico** | Ante el error `directory '/var/cache/bind' is not writable`, la IA relacionó el *tmpfs*, `cap_drop` y el cambio de usuario de `named` y propuso la corrección en un paso. |
| **Documentación y pruebas** | Genera README, comentarios y *scripts* de verificación, tareas que a menudo se quedan sin hacer. |
| **Automatización con agentes** | Un agente puede ejecutar el ciclo de construir, probar, leer el error y corregir sin intervención manual en cada paso. |
| **Traducción entre tecnologías** | Migrar un `dhcpd.conf` antiguo a `kea-dhcp4.conf`, o un `iptables` a `nftables`. |

### 5.2 Desventajas

| Desventaja | Ejemplo concreto |
|---|---|
| **Inseguro por defecto** | Con el *prompt* mínimo sale `MYSQL_ROOT_PASSWORD: admin123` y `3306:3306` publicado ([apartado 4](#4-análisis-técnico-de-diferencias)). |
| **Alucinaciones y obsolescencia** | Recomendar `isc-dhcp-server` o `listen … http2`, que parecen correctos pero están desfasados. |
| **Errores que solo se ven al ejecutar** | El *tmpfs* de BIND pasaba la validación sintáctica (`named-checkconf`) y fallaba en tiempo de ejecución. |
| **Falsa confianza** | Las pruebas «en verde» de la IA pueden medir otra cosa (el caso del DNS embebido de Docker). |
| **Privacidad** | Para diagnosticar suele pedirse el `named.conf` real, con IP y nombres internos que acaban en el proveedor. |
| **Dependencia y pérdida de habilidades** | Si el técnico no sabe qué hace `ChrootDirectory`, no sabrá por qué el SFTP rechaza la conexión cuando cambian los permisos del directorio. |
| **Coste y disponibilidad** | Las licencias por usuario, el consumo de *tokens* en agentes o una caída del servicio del proveedor bloquean el trabajo. |
| **Ambigüedad legal** | Licencias del código generado y responsabilidad ante un incidente. |

**Balance:** las ventajas se aprovechan cuando **quien usa la IA sabe
evaluarla**. Las desventajas crecen cuanto menos experiencia tiene el usuario y
cuanto menos se verifica.

---

## 6. Informe crítico

### 6.1 Qué ha demostrado el caso práctico

1. **La IA acelera mucho, pero no sustituye al criterio técnico.** El código final
   es sólido porque se exigieron requisitos concretos (versiones, mínimo
   privilegio, TLS, SFTP con clave) y porque **se probó todo**. Con un *prompt*
   genérico, la misma IA produce una infraestructura funcional pero insegura.
2. **La validación sintáctica no basta.** Las cuatro configuraciones pasaron sus
   validadores y, aun así, el DNS no arrancaba. Hacen falta **pruebas de
   comportamiento**: que el servicio responda, que deniegue lo que debe denegar y
   que se integre con los demás.
3. **Hay que auditar también las pruebas.** Dos pruebas generadas medían algo
   distinto de lo que decían medir. Una prueba mal diseñada es peor que no tener
   ninguna, porque da una seguridad falsa.
4. **El valor está en el ciclo, no en la respuesta.** Generar, validar, probar,
   corregir y documentar: la IA encaja muy bien en ese ciclo si hay un humano que
   define los criterios de aceptación.

### 6.2 Valoración

| Dimensión | Valoración | Comentario |
|---|---|---|
| Rapidez | ★★★★★ | Mejora clarísima en tareas de estructura y de configuración estándar. |
| Corrección a la primera | ★★★☆☆ | Buena en lo común; falla en detalles de entorno (permisos, Docker, versiones). |
| Seguridad por defecto | ★★☆☆☆ | Depende casi por completo del *prompt* y de la revisión. |
| Aprendizaje | ★★★★☆ | Excelente si se pide que explique; perjudicial si solo se copia y pega. |
| Riesgo para los datos | Alto | Si se usan herramientas públicas con información interna. |

### 6.3 ¿Cuándo usarla y cuándo no?

- ✅ **Sí:** borradores de configuración, *scripts* repetitivos, migraciones de
  sintaxis, documentación, casos de prueba, explicaciones y lluvia de ideas para
  diagnosticar.
- ⚠️ **Con cautela:** cambios en producción, reglas de cortafuegos, criptografía,
  gestión de identidades y acceso (IAM) y cualquier tarea en la que un error
  silencioso sea caro.
- ❌ **No:** introducir secretos, datos personales o información clasificada en
  herramientas no aprobadas; tomar decisiones automatizadas sobre personas sin
  supervisión; aplicar cambios sin revisarlos.

### 6.4 Conclusión

La IA generativa ya es una herramienta estándar en los entornos TIC, comparable
a lo que supusieron los buscadores o Stack Overflow, pero con mucho más alcance y
con riesgos nuevos. Su uso responsable exige **tres cosas**: **competencia
técnica** para evaluar lo que produce, **procesos de verificación**
automatizados y **un marco de gobierno** que proteja los datos y cumpla la
normativa. Sin ellas, la IA no reduce el trabajo: lo traslada a la resolución de
incidentes.

---

## 7. Recomendaciones alineadas con la normativa

### 7.1 Marco normativo aplicable

| Norma | Qué exige y cómo afecta al uso de IA en TIC |
|---|---|
| **Reglamento (UE) 2024/1689 de Inteligencia Artificial (RIA o *AI Act*)** | Enfoque por niveles de riesgo. Desde el **2 de febrero de 2025** rigen las prácticas prohibidas y la obligación de **alfabetización en IA** del personal (art. 4). Desde el **2 de agosto de 2025**, las obligaciones de los modelos de propósito general. Desde el **2 de agosto de 2026**, la mayoría del resto, incluidas las obligaciones de **transparencia** (art. 50). Usar IA para generar configuraciones no es de alto riesgo, pero usarla en selección de personal o en infraestructuras críticas puede serlo. La Comisión propuso en noviembre de 2025 (Ómnibus Digital) aplazar parte de las obligaciones de alto riesgo: conviene comprobar el calendario vigente. |
| **RGPD (UE 2016/679) y LOPDGDD (LO 3/2018)** | Base jurídica para tratar datos personales con IA; **privacidad desde el diseño** (art. 25); **contrato de encargado** con el proveedor (art. 28); **seguridad** (art. 32); **EIPD** cuando el riesgo es alto (art. 35); garantías en **transferencias internacionales** (arts. 44 y siguientes). |
| **Esquema Nacional de Seguridad (RD 311/2022)** | Obligatorio para el sector público y sus proveedores: análisis de riesgos, control de accesos, registro de actividad y gestión de proveedores, incluidos los de IA en la nube. |
| **Directiva NIS2 (UE 2022/2555)** | Gestión de riesgos de ciberseguridad y de la **cadena de suministro** en entidades esenciales e importantes (en proceso de transposición en España). Afecta a la elección de proveedores de IA y a las dependencias que la IA introduce. |
| **Reglamento de Ciberresiliencia (UE 2024/2847)** | Seguridad desde el diseño en productos con elementos digitales. Obligaciones de notificación desde el **11 de septiembre de 2026** y aplicación plena el **11 de diciembre de 2027**. El software generado con IA que se comercializa debe cumplirlo igual. |
| **Ley de Propiedad Intelectual (RDL 1/1996)** | Respetar las licencias del código que la IA pueda reproducir y aclarar la titularidad del código generado. |
| **Supervisión** | La **AESIA** (Agencia Española de Supervisión de la IA) es la autoridad de vigilancia del RIA en España; la **AEPD**, en protección de datos. |
| **Normas y guías de referencia** | ISO/IEC 42001:2023 (sistema de gestión de IA), NIST AI RMF 1.0, OWASP Top 10 para aplicaciones con LLM (2025). |

### 7.2 Recomendaciones

Se ordenan por prioridad. Todas son **viables para una pyme**, con coste bajo o
nulo.

| # | Recomendación | Cómo aplicarla | Norma con la que se alinea |
|---|---|---|---|
| R1 | **Política de uso de IA** por escrito, breve y conocida por todo el personal. | Una o dos páginas: herramientas aprobadas, datos prohibidos, revisión obligatoria y a quién consultar. Revisión anual. | RIA art. 4; RGPD art. 24; ISO/IEC 42001 |
| R2 | **Clasificar la información** y fijar qué nivel puede ir a cada herramienta. | Público → cualquier herramienta. Interno → solo herramientas corporativas. Confidencial, secretos o datos personales → nunca, o solo con modelo local o EIPD previa. | RGPD arts. 5.1.f y 32; ENS |
| R3 | **Herramientas corporativas con contrato**, no cuentas personales gratuitas. | Planes de empresa con cláusula de **no entrenamiento** con los datos, DPA (art. 28), ubicación de los datos en la UE o garantías de transferencia, y SSO. Para datos muy sensibles, un **modelo de pesos abiertos en local**. | RGPD arts. 28 y 44; NIS2 (cadena de suministro) |
| R4 | **Revisión humana obligatoria y verificación automatizada** de todo lo generado antes de producción. | Validadores (`nginx -t`, `named-checkconf`…), `shellcheck`, SAST (Semgrep, CodeQL), escáner de secretos (*gitleaks*), escáner de imágenes (Trivy) y pruebas de comportamiento en CI, como `verificar.sh`. | RGPD art. 25; ENS; CRA |
| R5 | **Seguridad de la cadena de suministro.** | Fijar versiones, verificar que cada paquete o imagen sugerido existe y es el oficial, y generar SBOM (Syft) para detectar el *slopsquatting*. | NIS2; CRA |
| R6 | **Mínimo privilegio para los agentes de IA.** | Ejecutarlos en contenedores o máquinas virtuales aisladas, sin credenciales de producción, con aprobación humana para las acciones irreversibles y con registro de todo lo que hacen. | OWASP LLM06; ENS (control de acceso y trazabilidad) |
| R7 | **Formación (alfabetización en IA).** | Sesión inicial y anual: cómo funciona un LLM, alucinaciones, inyección de *prompts*, qué datos no se comparten y cómo verificar. Guardar un registro de asistencia como evidencia. | RIA art. 4 (en vigor desde el 2 de febrero de 2025) |
| R8 | **Transparencia y trazabilidad.** | Declarar el uso de IA en documentos y *commits* relevantes (como en este informe) y etiquetar el contenido sintético dirigido al público. | RIA art. 50 |
| R9 | **Inventario de usos de IA y evaluación de riesgos.** | Una hoja con cada uso (herramienta, finalidad, datos, responsable y nivel de riesgo según el RIA). Hacer una EIPD si hay datos personales a gran escala o decisiones sobre personas. | RIA (clasificación de riesgo); RGPD art. 35 |
| R10 | **Revisar las licencias y la titularidad** del código generado. | Activar filtros de coincidencia con código público cuando la herramienta los tenga y revisar los fragmentos largos. Dejar clara la titularidad en los contratos con clientes. | LPI |
| R11 | **Plan de contingencia.** | Que el trabajo crítico no dependa de la disponibilidad de un único proveedor de IA y que haya procedimientos manuales documentados. | NIS2 (continuidad); ENS |
| R12 | **Medir y revisar.** | Indicadores como incidentes causados por código generado, tiempo ahorrado y hallazgos en revisión. Revisar la política con esos datos. | ISO/IEC 42001 (mejora continua) |

### 7.3 Lista de comprobación antes de desplegar código generado con IA

- [ ] No he introducido secretos, datos personales ni información confidencial en la herramienta.
- [ ] Entiendo cada línea: podría explicarla y mantenerla sin la IA.
- [ ] Las versiones están fijadas y las dependencias existen y son oficiales.
- [ ] La configuración pasa los validadores de cada servicio.
- [ ] Hay pruebas de comportamiento, incluidas las que comprueban lo que **debe fallar** (accesos denegados).
- [ ] He revisado qué mide realmente cada prueba.
- [ ] Secretos fuera del repositorio, mínimo privilegio y mínima exposición de puertos.
- [ ] El uso de IA está declarado donde corresponde.

---

## 8. Referencias

- Reglamento (UE) 2024/1689 del Parlamento Europeo y del Consejo, de 13 de junio de 2024, de Inteligencia Artificial. DOUE L, 12/07/2024.
- Reglamento (UE) 2016/679 (RGPD) y Ley Orgánica 3/2018 (LOPDGDD).
- Real Decreto 311/2022, por el que se regula el Esquema Nacional de Seguridad.
- Directiva (UE) 2022/2555 (NIS2) y Reglamento (UE) 2024/2847 (Ciberresiliencia).
- Real Decreto 729/2023, Estatuto de la Agencia Española de Supervisión de la Inteligencia Artificial.
- ISO/IEC 42001:2023. *Artificial intelligence — Management system*.
- NIST (2023). *AI Risk Management Framework (AI RMF 1.0)*.
- OWASP (2025). *Top 10 for Large Language Model Applications*.
- Vaswani, A. et al. (2017). *Attention Is All You Need*. NeurIPS.
- Perry, N., Srivastava, M., Kumar, D., Boneh, D. (2023). *Do Users Write More Insecure Code with AI Assistants?* ACM CCS.
- Peng, S. et al. (2023). *The Impact of AI on Developer Productivity: Evidence from GitHub Copilot*. arXiv:2302.06590.
- Spracklen, J. et al. (2025). *We Have a Package for You! A Comprehensive Analysis of Package Hallucinations by Code Generating LLMs*. USENIX Security.
- ISC. *ISC DHCP End of Life* (2022) y documentación de Kea DHCP.
- ICANN (2024). Reserva del TLD `.internal` para uso privado. RFC 6762 (mDNS y `.local`).
- Documentación oficial de BIND 9, Nginx (directiva `http2`, 1.25.1), MariaDB 11.4 y OpenSSH `sshd_config(5)`.

---

## Anexo A. Prompts utilizados

**Versión A (mínimo):**

> Hazme un docker-compose con DNS, DHCP, web, base de datos y FTP para una empresa.

**Versión B (con requisitos):**

> Actúa como administrador de sistemas. Genera una infraestructura con Docker
> Compose para la red 10.10.0.0/24 del dominio empresa.internal con: BIND 9
> (zona directa e inversa, recursión solo para la LAN, sin AXFR, versión
> oculta); ISC Kea DHCPv4 (pool .100–.199, DNS, puerta de enlace, dominio de
> búsqueda y reserva por MAC para la impresora); Nginx con HTTPS, redirección
> 80→443 y cabeceras de seguridad; MariaDB LTS con un usuario de aplicación de
> mínimo privilegio y sin puerto publicado; SFTP con OpenSSH enjaulado y solo
> clave pública. Requisitos: versiones fijadas, secretos fuera del código
> (Docker secrets), `cap_drop: ALL` y solo las capacidades necesarias,
> *healthchecks*, comentarios que expliquen el porqué y un script que
> verifique cada servicio, incluidos los accesos que deben denegarse. Valida
> cada configuración con la herramienta oficial del servicio.

**Iteraciones de corrección:** tras cada fallo se entregó a la IA la salida
literal del error (por ejemplo, `directory '/var/cache/bind' is not writable`)
para que diagnosticara la causa y la corrigiera. Las correcciones están en el
[apartado 4.3](#43-diferencias-entre-iteraciones-de-la-propia-ia-observadas).
