# Compilar y usar Mirror

## Resumen de la arquitectura

- **Motor:** [scrcpy](https://github.com/Genymobile/scrcpy) (open source) hace el
  espejo + control + apagado real del panel, vía ADB. Sin root.
- **Front-end (este repo):** una app Windows (.NET 8, WinForms) que envuelve
  scrcpy y añade: ventana **ingrabable**, **toggle** con Enter del numpad +
  NumLock y **animación** de deslizamiento, **volteo**, **contraseña** y
  **modo hotspot** ("solo desde tu red").

No hace falta compilar scrcpy: se usa el binario oficial.

---

## 1. Preparar el móvil (Samsung S24 FE) — una sola vez

1. **Ajustes → Acerca del teléfono → Información de software →** toca 7 veces
   "Número de compilación" para activar **Opciones de desarrollador**.
2. En **Opciones de desarrollador**, activa **Depuración USB**.
3. Conéctalo por USB al PC y acepta el diálogo **"¿Permitir depuración USB?"**
   marcando *Permitir siempre desde este ordenador*.

### (Opcional) Modo inalámbrico / hotspot — sin cable

En Android 11+ puedes usar ADB por Wi-Fi sin PC conectado:

1. En **Opciones de desarrollador → Depuración inalámbrica**, actívala.
2. La primera vez empareja: "Vincular dispositivo con código" te da IP:puerto y
   un código. Desde el PC: `adb pair IP:PUERTO` y metes el código.
3. Para conectar cada sesión: `adb connect IP:5555` (la IP del móvil en el
   hotspot; suele ser algo como `192.168.43.1`). Esa IP:puerto es la que pones
   como "Dirección ADB del móvil" en la configuración de Mirror.

> **Modo hotspot:** enciende el *Compartir Internet / Zona Wi-Fi* del móvil,
> conecta el PC a esa red, y usa la IP del móvil en esa red. Así la red vigilada
> del edificio queda totalmente fuera.

---

## 2. Compilar el front-end de Windows

Necesitas el **.NET 8 SDK** (https://dotnet.microsoft.com/download). En Windows:

```powershell
cd pc-windows
dotnet build -c Release
```

El ejecutable queda en `pc-windows\bin\Release\net8.0-windows\Mirror.exe`.
**No requiere administrador** (manifiesto `asInvoker`).

Para un único ejecutable portable:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -o publish
```

## 3. Añadir scrcpy + adb

1. Descarga scrcpy para Windows desde
   https://github.com/Genymobile/scrcpy/releases (el zip incluye `adb.exe`).
2. Crea una carpeta `tools` **junto a `Mirror.exe`** y copia dentro TODO el
   contenido del zip de scrcpy (`scrcpy.exe`, `adb.exe`, los `.dll` y el
   `scrcpy-server`).

Estructura final:

```
Mirror.exe
tools\
   scrcpy.exe
   adb.exe
   scrcpy-server
   *.dll
config.json   (se crea solo en %APPDATA%\Mirror)
```

---

## 4. Usar

1. Abre `Mirror.exe`.
2. **Primera vez:** crea la **contraseña** y elige el **modo de conexión**
   (USB recomendado). Si es hotspot/Wi-Fi, pon la dirección ADB del móvil.
3. Siguientes veces: mete la contraseña (5 intentos máx.).
4. El móvil aparece/desaparece con **Enter del numpad estando NumLock activado**,
   deslizándose desde la esquina inferior derecha.
5. Desde el **icono de bandeja**: Voltear/Rotar y Salir.

### Controles

| Acción | Cómo |
|--------|------|
| Mostrar/ocultar móvil | NumLock ON + Enter del numpad |
| Voltear / rotar | Menú de bandeja → "Voltear / Rotar" |
| Controlar el móvil | Ratón y teclado sobre la ventana (lo gestiona scrcpy) |
| Salir | Menú de bandeja → "Salir" |

### Ajustes finos (`%APPDATA%\Mirror\config.json`)

- `TurnScreenOff`: apaga el panel físico del móvil (true por defecto).
- `MaxSize` / `BitrateMbps` / `MaxFps`: calidad vs. latencia. Si va justo por
  Wi-Fi, baja `MaxSize` a 1024 y el bitrate a 4–6.
- `RequiredSsid`: si lo rellenas (modo hotspot), Mirror **solo** arranca cuando
  el PC está en esa red.
- `OrientationFlagFormat`: si tu versión de scrcpy rechaza `--orientation`,
  cámbialo a `--lock-video-orientation={0}` o `--capture-orientation={0}`.
- `Trigger`: `NumpadEnterNumLock` (por defecto) o `Hotkey` (usa `ToggleHotkey`).

---

## Solución de problemas

- **"No encuentro scrcpy.exe / adb.exe"** → falta la carpeta `tools` (paso 3).
- **La ventana no aparece** → el móvil no está autorizado. Ejecuta
  `tools\adb.exe devices` y acepta el diálogo en el móvil.
- **La grabación NO sale negra** → tu Windows es anterior a 10 versión 2004;
  `WDA_EXCLUDEFROMCAPTURE` necesita esa versión o superior.
- **Una app concreta se ve negra en el espejo** → tiene `FLAG_SECURE` (banca,
  Netflix). Es Android quien lo bloquea, no Mirror.
