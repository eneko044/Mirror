# Modelo de seguridad

Lee esto entero. La parte más importante es "Lo que NO protege".

## Raíz de confianza: tu contraseña

La primera vez defines una contraseña en el móvil. De ella se deriva, con
**Argon2id** (memoria alta, resistente a fuerza bruta por GPU), una clave
pre-compartida (PSK). Esa misma contraseña la introduces en el PC.

El handshake es un **Noise-like NNpsk0**:

1. Ambos extremos generan un par efímero **X25519** (nuevo en cada conexión →
   *forward secrecy*: comprometer la contraseña mañana no descifra el tráfico
   grabado hoy, si no se conocía la contraseña en ese momento).
2. Intercambian las claves públicas efímeras.
3. Derivan un secreto compartido ECDH y lo mezclan con la **PSK** derivada de la
   contraseña mediante HKDF.
4. Se confirman mutuamente con un tag de autenticación. Si la contraseña no
   coincide en ambos lados, el handshake **falla** y no se transmite nada.

Consecuencia: un atacante en medio de la red (MITM) **no puede** hacerse pasar
por ninguno de los extremos sin conocer la contraseña, aunque controle el router.

## Cifrado del flujo

Tras el handshake, todo (vídeo y eventos de entrada) viaja con
**ChaCha20-Poly1305** (AEAD), con nonce incremental por dirección. Cada mensaje
está autenticado: un atacante no puede modificar ni reordenar sin que se detecte.

## Protección contra captura de pantalla

- **PC (Windows):** la ventana llama a
  `SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE)`. Las APIs de captura
  del sistema (PrintScreen, Game Bar, OBS por Desktop/Window Capture, la mayoría
  de software de grabación y de escritorio remoto) obtienen **negro** para esa
  ventana. Es una API documentada de Microsoft.
- **Android:** cada ventana de la app usa `FLAG_SECURE`. Las capturas y
  grabaciones del sistema salen en negro sobre la app.

## Lo que NO protege  ← LÉELO

1. **Cámara externa / foto.** Nada de esto impide que alguien fotografíe tu
   pantalla con otro dispositivo. La protección es solo contra captura por
   software en la propia máquina.
2. **Captura por hardware.** Una capturadora en el cable HDMI/DisplayPort, o un
   monitor/KVM que grabe, ve la imagen real. `WDA_EXCLUDEFROMCAPTURE` actúa en
   la composición del escritorio, no en la salida física.
3. **Malware con privilegios en tu propia máquina.** Si el PC ya está
   comprometido a nivel de kernel/driver de vídeo, o hay un keylogger, esta
   herramienta no te salva. Protege frente a *observadores externos*, no frente a
   un equipo ya intervenido.
4. **Metadatos de red.** Un observador ve que hay una conexión cifrada entre dos
   IPs y su volumen/tiempos. No ve el contenido. Ocultar la *existencia* del
   flujo no es objetivo de este proyecto.
5. **Análisis forense del disco.** Esto no borra rastros locales (logs del SO,
   etc.). No es una herramienta anti-forense.

## Recomendaciones de uso

- Contraseña larga y única (frase de varias palabras). Argon2id la protege, pero
  una contraseña débil sigue siendo el eslabón débil.
- Úsalo en la **red local** (móvil y PC en el mismo Wi-Fi/LAN). No expongas el
  puerto a Internet sin un túnel adicional.
- Si de verdad tu red tiene vigilancia activa, asume que el metadato de "hay
  tráfico cifrado" es visible y decide si eso es aceptable para ti.

## Consideración legal

Usa esto en dispositivos y redes que sean tuyos o donde tengas permiso. Ocultar
actividad frente a monitorización a la que estás legítimamente sujeto (equipo de
trabajo, controles parentales, medidas judiciales) puede tener consecuencias
legales o contractuales. La herramienta es neutral; el uso es tuyo.
