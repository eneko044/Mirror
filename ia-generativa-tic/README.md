# IA generativa en entornos TIC

Trabajo sobre el uso de la IA generativa en entornos TIC, con un caso práctico
real y verificado.

- **[INFORME.md](INFORME.md)**: el informe completo. Cada apartado corresponde a
  un criterio de la rúbrica:

  | Criterio | Apartado |
  |---|---|
  | Identificación de conceptos de IA generativa | 1 |
  | Reconocimiento de riesgos y limitaciones | 2 |
  | Generación de código funcional con IA | 3 y [`servicios/`](servicios/) |
  | Análisis técnico de diferencias | 4 |
  | Identificación de ventajas y desventajas | 5 |
  | Elaboración de informe crítico | 6 |
  | Propuesta de recomendaciones | 7 |

- **[servicios/](servicios/)**: infraestructura con Docker Compose (DNS BIND9,
  DHCP Kea, web Nginx, MariaDB y SFTP) y sus pruebas.

```bash
cd servicios
./generar-secretos.sh
./verificar.sh            # construye, arranca y ejecuta 28 pruebas
```

Requisitos: Docker con Compose v2, `openssl` y `ssh-keygen`.
