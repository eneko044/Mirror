# Mirror

Espejo (mirror) y control remoto de un móvil Android desde un PC Windows, con
foco en privacidad sobre redes no confiables. Misma idea que scrcpy / Vysor /
Android Auto proyectado, pero envuelto en un front-end discreto: ventana
**ingrabable**, toggle con **Enter del numpad + NumLock** y animación, volteo,
contraseña y **modo hotspot**.

> **Lee `docs/SECURITY.md` antes de confiar en esto para nada serio.** Ahí está
> el modelo de amenazas y, sobre todo, lo que este proyecto **NO** protege.

## Arquitectura

- **Motor:** [scrcpy](https://github.com/Genymobile/scrcpy) (open source) hace el
  espejo + control + **apagado real del panel** del móvil, vía **ADB**. Sin root,
  sin tocar Knox, y funciona con **todas** tus apps.
- **Front-end (este repo):** app Windows (.NET 8, WinForms) que envuelve scrcpy y
  añade toda la capa de privacidad y la experiencia de uso.

## Qué hace

- 🪟 **Ventana ingrabable:** `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)`.
  Grabar la pantalla del PC o hacer captura → esa ventana sale **negra**.
- ⌨️ **Toggle discreto:** con **NumLock activado**, el **Enter del numpad** hace
  aparecer/desaparecer el móvil deslizándose desde la esquina inferior derecha.
  Con NumLock apagado, ese Enter funciona normal.
- 🔄 **Voltear / rotar** desde el icono de bandeja.
- 🔒 **Contraseña** (Argon2id) con bloqueo por intentos.
- 🔌 **Modo hotspot:** conectas el PC al hotspot del móvil → la red vigilada del
  edificio queda **totalmente fuera del circuito**. Opción de "solo desde esa
  red" (`RequiredSsid`).
- 🚫 Sin permisos de administrador. Sin rastro sensible en disco.

## Qué NO hace (importante)

- **No** te hace invisible. Una **foto con otra cámara**, o una **capturadora de
  hardware** en el cable de vídeo, siguen viendo la pantalla. La protección es
  contra captura **por software** en la máquina.
- **No** oculta el *metadato* de que existe una conexión (salvo en modo hotspot/
  USB, donde la red vigilada simplemente no ve nada). No es una herramienta para
  derrotar sistemas de vigilancia; es para que tu contenido sea ilegible.
- Apps con `FLAG_SECURE` (banca, Netflix) se ven **negras** en el espejo: lo
  bloquea Android, no Mirror.

## Estructura

```
pc-windows/      App Windows (C# .NET 8, WinForms) -> genera Mirror.exe
docs/            SECURITY.md, PROTOCOL.md, BUILD.md
```

## Empezar

Todo el detalle (preparar el móvil, compilar, añadir scrcpy, usar) está en
**`docs/BUILD.md`**.

Resumen:
1. Activa Depuración USB en el móvil (o Depuración inalámbrica para hotspot).
2. `cd pc-windows && dotnet build -c Release`.
3. Copia scrcpy+adb en una carpeta `tools` junto a `Mirror.exe`.
4. Abre `Mirror.exe`, crea la contraseña, elige modo. NumLock + Enter del numpad
   para mostrar/ocultar.
