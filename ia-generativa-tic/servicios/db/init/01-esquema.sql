-- Se ejecuta solo la primera vez que se crea el volumen de datos.
USE inventario;

CREATE TABLE equipos (
    id           INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    nombre       VARCHAR(64)  NOT NULL UNIQUE,
    tipo         ENUM('pc','portatil','impresora','servidor','red') NOT NULL,
    ip           VARCHAR(15),
    alta         TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO equipos (nombre, tipo, ip) VALUES
    ('ns1',       'servidor',  '10.10.0.53'),
    ('web',       'servidor',  '10.10.0.80'),
    ('impresora', 'impresora', '10.10.0.50');
