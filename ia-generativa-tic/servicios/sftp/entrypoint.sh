#!/bin/sh
# Genera las claves de host UNA vez y las guarda en un volumen: si se
# regeneraran en cada arranque, los clientes verían un aviso de MITM.
set -eu
dir=/etc/ssh/claves_host
[ -f "$dir/ssh_host_ed25519_key" ] || ssh-keygen -q -t ed25519 -N '' -f "$dir/ssh_host_ed25519_key"
[ -f "$dir/ssh_host_rsa_key" ]     || ssh-keygen -q -t rsa -b 3072 -N '' -f "$dir/ssh_host_rsa_key"
chown intercambio:sftpusuarios /srv/sftp/intercambio/subidas
/usr/sbin/sshd -t
exec /usr/sbin/sshd -D -e
