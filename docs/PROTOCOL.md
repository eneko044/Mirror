# Protocolo de red

Transporte: **TCP** (baja latencia en LAN con `TCP_NODELAY`). El móvil actúa como
**servidor** (escucha), el PC como **cliente** (conecta). Puerto por defecto
**7345**.

Todos los enteros son **big-endian**. Un "frame" del protocolo es:

```
[ u32 longitud ][ cuerpo (longitud bytes) ]
```

## 1. Handshake (Noise NNpsk0 simplificado)

Constantes:
- KDF de contraseña: **Argon2id**, salt fijo por app + `t=3, m=64MiB, p=1`,
  salida 32 bytes → `PSK`.
- Curva: **X25519**. AEAD: **ChaCha20-Poly1305**. Hash/HKDF: **SHA-256**.
- Etiqueta de protocolo: la cadena ASCII `Mirror/1 NNpsk0`.

Pasos (e = clave efímera):

```
Cliente (PC)                        Servidor (móvil)
  genera e_c (X25519)
  --> msg1: e_c.pub (32 bytes)
                                     genera e_s (X25519)
                                     ss = X25519(e_s.priv, e_c.pub)
                                     k  = HKDF(PSK, ss, label) -> 32 bytes
                                     tag_s = AEAD_seal(k, nonce=0, "", h)  (solo tag, 16 bytes)
  <-- msg2: e_s.pub (32) || tag_s (16)
  ss = X25519(e_c.priv, e_s.pub)
  k  = HKDF(PSK, ss, label)
  verifica tag_s con AEAD_open(k, nonce=0, ...)   // si falla -> contraseña incorrecta, abortar
  tag_c = AEAD_seal(k, nonce=1, "", h)
  --> msg3: tag_c (16)
                                     verifica tag_c  // si falla -> abortar
```

`h` (hash de transcripción) = SHA-256(label || e_c.pub || e_s.pub). Se usa como
Associated Data del AEAD para atar los tags a esta sesión concreta (anti-replay
entre sesiones).

Tras el handshake se derivan **dos** claves de sesión direccionales con HKDF:
- `k_p2c` (móvil→PC, vídeo)
- `k_c2p` (PC→móvil, entrada)

Cada dirección lleva su propio contador de nonce de 96 bits que empieza en 0 y se
incrementa por mensaje. **Nunca** se reutiliza un nonce con la misma clave.

## 2. Mensajes de sesión (cifrados)

Cada frame de sesión, ya descifrado, empieza por un byte de tipo:

| tipo | nombre        | dirección | cuerpo |
|------|---------------|-----------|--------|
| 0x01 | VIDEO_CONFIG  | móvil→PC  | SPS/PPS H.264 (csd) |
| 0x02 | VIDEO_FRAME   | móvil→PC  | u64 pts_us, u8 flags(keyframe), NAL units |
| 0x10 | INPUT_TOUCH   | PC→móvil  | u8 action(0=down,1=move,2=up), i32 x, i32 y |
| 0x11 | INPUT_KEY     | PC→móvil  | u8 action, i32 keycode, i32 meta |
| 0x12 | INPUT_SCROLL  | PC→móvil  | i32 x, i32 y, i32 hscroll, i32 vscroll |
| 0x13 | INPUT_TEXT    | PC→móvil  | utf8 (para pegar texto) |
| 0x20 | CTRL_SCREEN_OFF | PC→móvil| u8 on(1)/off(0)  -> overlay negro |
| 0x21 | CTRL_ORIENT   | móvil→PC  | u8 rotation (0/1/2/3), u16 w, u16 h |
| 0x30 | PING/PONG     | ambos     | u64 nonce |

Las coordenadas de entrada van en el espacio del vídeo (el PC las envía en la
resolución del stream) y el móvil las escala a píxeles reales.

## 3. Latencia

- Encoder configurado con bitrate objetivo y `KEY_FRAME_INTERVAL` moderado,
  `latency`/`priority` en modo realtime cuando el dispositivo lo soporta.
- `TCP_NODELAY` en ambos extremos.
- El PC decodifica y presenta sin buffer de reordenación (los frames llegan en
  orden por TCP).
