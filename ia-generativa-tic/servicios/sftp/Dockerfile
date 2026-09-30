FROM ubuntu:24.04
RUN apt-get update \
 && apt-get install -y --no-install-recommends openssh-server procps \
 && rm -rf /var/lib/apt/lists/* /etc/ssh/ssh_host_* \
 && groupadd --system sftpusuarios \
 && useradd --create-home --home-dir /srv/sftp/intercambio --gid sftpusuarios \
            --shell /usr/sbin/nologin intercambio \
 && usermod -p '*' intercambio \
 && chown root:root /srv/sftp/intercambio && chmod 755 /srv/sftp/intercambio \
 && mkdir -p /srv/sftp/intercambio/subidas /etc/ssh/authorized_keys /etc/ssh/claves_host /run/sshd \
 && chown intercambio:sftpusuarios /srv/sftp/intercambio/subidas
COPY sshd_config /etc/ssh/sshd_config
COPY entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod 755 /usr/local/bin/entrypoint.sh
EXPOSE 22
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
