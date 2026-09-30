FROM ubuntu:24.04
RUN apt-get update \
 && apt-get install -y --no-install-recommends kea-dhcp4-server procps \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /run/kea /var/lib/kea
COPY kea-dhcp4.conf /etc/kea/kea-dhcp4.conf
# Falla la construcción si el JSON o la semántica de la configuración no son válidos.
RUN kea-dhcp4 -t /etc/kea/kea-dhcp4.conf
EXPOSE 67/udp
CMD ["kea-dhcp4", "-c", "/etc/kea/kea-dhcp4.conf"]
