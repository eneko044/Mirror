FROM ubuntu:24.04
RUN apt-get update \
 && apt-get install -y --no-install-recommends bind9 bind9-dnsutils \
 && rm -rf /var/lib/apt/lists/*
COPY named.conf /etc/bind/named.conf
COPY zones/ /etc/bind/zones/
# Falla la construcción de la imagen si la configuración o las zonas tienen errores.
RUN named-checkconf -z /etc/bind/named.conf
EXPOSE 53/udp 53/tcp
# -g: primer plano y logs a stderr (los recoge Docker). -u bind: suelta root.
CMD ["named", "-g", "-u", "bind", "-c", "/etc/bind/named.conf"]
