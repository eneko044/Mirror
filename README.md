# Mirror

Espejo (mirror) y control remoto de un móvil Android desde un PC Windows, con
transporte cifrado de extremo a extremo y ventana protegida contra captura de
pantalla. Misma categoría que scrcpy / Vysor / Android Auto proyectado, pero con
foco en privacidad sobre redes no confiables.

> **Lee `docs/SECURITY.md` antes de confiar en esto para nada serio.** Ahí está
> el modelo de amenazas y, sobre todo, lo que este proyecto **NO** protege.

## Qué hace

- El móvil captura su pantalla (`MediaProjection`, sin root) y la codifica en
  H.264 por hardware (`MediaCodec`) con baja latencia.
- El flujo viaja **cifrado** (X25519 + ChaCha20-Poly1305, clave raíz derivada de
  tu contraseña con Argon2id) por la red local.
- El PC lo descifra, lo decodifica (FFmpeg) y lo muestra en una ventana.
- Desde el PC controlas el móvil con ratón y teclado. El móvil ejecuta los
  toques/gestos mediante un **Servicio de Accesibilidad** (sin root).
- La ventana del PC usa `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)` y la
  app de Android usa `FLAG_SECURE`: las grabadoras/capturas por software del
  propio sistema ven **negro** en esa ventana.
- Modo "pantalla apagada" en el móvil: overlay negro a pantalla completa (parece
  apagada) mientras sigues interactuando desde el PC.

## Qué NO hace (importante)

- **No** te hace invisible. Una foto con otra cámara, o una capturadora de
  hardware en el cable de vídeo, siguen viendo la pantalla. La protección es
  contra captura **por software** en la máquina.
- **No** apaga el panel físico del móvil de verdad sin root/ADB. El "apagado" es
  un overlay.
- **No** oculta *que existe una conexión*. Un observador de red ve que dos
  dispositivos intercambian tráfico cifrado (no ve el contenido, pero ve el
  flujo). Si necesitas ocultar también el metadato, eso es otro problema (Tor,
  túneles) y está fuera del alcance de esta herramienta.

## Estructura

```
protocol/        Especificación e implementación del handshake y cifrado (compartido)
android-app/     App Android (Kotlin, Gradle)  -> genera el APK
pc-windows/      App Windows (C# .NET 8, WinForms) -> genera el EXE
docs/            SECURITY.md (modelo de amenazas), PROTOCOL.md, BUILD.md
```

## Compilar

No se compila en este repositorio de desarrollo (es Linux). Cada parte se
compila en su entorno:

- **APK:** ver `docs/BUILD.md` → sección Android. Necesitas Android Studio o el
  SDK + Gradle.
- **EXE:** ver `docs/BUILD.md` → sección Windows. Necesitas .NET 8 SDK. No
  requiere permisos de administrador para ejecutarse.

## Uso rápido

1. Instala el APK en el móvil y el EXE en el PC (misma red).
2. Primera vez: la app te pide crear una **contraseña**. La misma contraseña se
   introduce en el PC. Esa contraseña es la raíz de confianza del cifrado.
3. En el móvil pulsa "Mirror online". En el PC, introduce IP + contraseña y
   conecta.
4. Concede a la app de Android el permiso de captura y activa el Servicio de
   Accesibilidad (una sola vez).

## Estado

Proyecto base funcional. Cada módulo indica en su cabecera qué está
implementado y qué requiere pruebas en dispositivo real. Ver
`docs/BUILD.md` para el detalle.
