#!/bin/bash
# Usuario de la aplicación con el MÍNIMO privilegio: solo CRUD sobre
# "inventario" y solo desde la LAN. Nunca se usa root desde las aplicaciones.
set -euo pipefail
APP_PASS="$(cat /run/secrets/db_app_password)"
# Escapa comillas simples para el literal SQL.
APP_PASS_SQL="${APP_PASS//\'/\'\'}"

mariadb --protocol=socket -uroot -p"$(cat /run/secrets/db_root_password)" <<SQL
CREATE USER 'app'@'10.10.0.%' IDENTIFIED BY '${APP_PASS_SQL}';
GRANT SELECT, INSERT, UPDATE, DELETE ON inventario.* TO 'app'@'10.10.0.%';
FLUSH PRIVILEGES;
SQL
