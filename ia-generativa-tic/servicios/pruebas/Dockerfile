# Imagen cliente usada solo por verificar.sh
FROM ubuntu:24.04
RUN apt-get update \
 && apt-get install -y --no-install-recommends \
      bind9-dnsutils curl ca-certificates mariadb-client openssh-client \
 && rm -rf /var/lib/apt/lists/*
