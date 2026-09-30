# ============================================================
#  LocalPanel - activar ADB por red con UN cable (una vez)
#  Alternativa cuando la "Depuracion inalambrica" de Samsung se
#  bloquea por no estar en una Wi-Fi. Este metodo NO la necesita:
#  cable 10 segundos y luego vas sin cable, incluso por el hotspot
#  del movil (la red del edificio queda fuera).
# ============================================================
$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

$adb = Join-Path $PSScriptRoot "publish\tools\adb.exe"
if (-not (Test-Path $adb)) {
    Write-Host "No encuentro adb en publish\tools. Ejecuta build.bat o copia scrcpy ahi." -ForegroundColor Red
    exit 1
}

Write-Host "=== Activar ADB por red (un cable, una vez) ===" -ForegroundColor Cyan
Write-Host "En el movil: Opciones de desarrollador > Depuracion USB (ON)."
Read-Host "Conecta el movil por USB, acepta 'Permitir depuracion USB', y pulsa Enter"

& $adb devices
Write-Host ""
Write-Host "Si arriba tu movil NO sale como 'device', acepta el dialogo en el movil."
Read-Host "Cuando salga como 'device', pulsa Enter para activar el modo red"

& $adb tcpip 5555
if ($LASTEXITCODE -ne 0) {
    Write-Host "Fallo al activar el modo red. Revisa la conexion USB." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Modo red activado. Ahora:" -ForegroundColor Green
Write-Host "  1) Desconecta el cable USB."
Write-Host "  2) Enciende el HOTSPOT del movil (Zona Wi-Fi)."
Write-Host "  3) Conecta ESTE PC a ese hotspot."
Read-Host "Cuando el PC este conectado al hotspot del movil, pulsa Enter"

# La IP del movil en su hotspot = puerta de enlace por defecto del PC
$gw = (Get-NetRoute -DestinationPrefix '0.0.0.0/0' -ErrorAction SilentlyContinue |
       Sort-Object RouteMetric | Select-Object -First 1).NextHop
if ($gw) { Write-Host "IP detectada del movil (hotspot): $gw" -ForegroundColor Yellow }
$ip = Read-Host "IP del movil [$gw] (Enter para aceptar la detectada)"
if ([string]::IsNullOrWhiteSpace($ip)) { $ip = $gw }
if ([string]::IsNullOrWhiteSpace($ip)) {
    Write-Host "Sin IP. Mira en el movil la IP del hotspot y reintenta." -ForegroundColor Red
    exit 1
}

$addr = "${ip}:5555"
& $adb connect $addr
if ($LASTEXITCODE -ne 0) {
    Write-Host "No conecto a $addr." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== LISTO ===" -ForegroundColor Green
Write-Host "En LocalPanel (primera vez) elige modo 'Hotspot' y pon como direccion:"
Write-Host "   $addr" -ForegroundColor Yellow
Write-Host ""
Write-Host "Nota: si REINICIAS el movil, repite este paso del cable (el modo red se resetea)."
