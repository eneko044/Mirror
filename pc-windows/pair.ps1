# ============================================================
#  LocalPanel - emparejar el movil por Wi-Fi (SIN cable, una vez)
#  Requiere haber ejecutado antes build.bat (para tener adb en publish\tools).
# ============================================================
$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

$adb = Join-Path $PSScriptRoot "publish\tools\adb.exe"
if (-not (Test-Path $adb)) {
    Write-Host "No encuentro adb en publish\tools. Ejecuta antes build.bat." -ForegroundColor Red
    exit 1
}

Write-Host "=== Emparejar movil por Wi-Fi (sin cable) ===" -ForegroundColor Cyan
Write-Host "PC y movil deben estar en la MISMA red (o el PC conectado al hotspot del movil)."
Write-Host ""
Write-Host "En el movil: Ajustes > Opciones de desarrollador > Depuracion inalambrica (ON)."
Write-Host "Dentro, abre 'Vincular dispositivo con codigo de vinculacion'."
Write-Host ""

$pairAddr = Read-Host "1) 'Direccion IP y puerto' de ESE dialogo (ej 192.168.1.50:37123)"
$code     = Read-Host "2) Codigo de 6 digitos que muestra"
Write-Host "Emparejando..."
& $adb pair $pairAddr $code
if ($LASTEXITCODE -ne 0) {
    Write-Host "Fallo el emparejamiento. Revisa direccion y codigo (caducan rapido)." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Cierra ese dialogo. En la pantalla principal de 'Depuracion inalambrica'"
Write-Host "aparece OTRA 'Direccion IP y puerto' (puerto distinto)."
$connAddr = Read-Host "3) Escribela aqui (ej 192.168.1.50:39555)"
& $adb connect $connAddr
if ($LASTEXITCODE -ne 0) {
    Write-Host "No conecto. Revisa la direccion." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== LISTO ===" -ForegroundColor Green
Write-Host "En LocalPanel (primera vez) elige modo 'Hotspot' o 'Wi-Fi' y pon como direccion:"
Write-Host "   $connAddr" -ForegroundColor Yellow
Write-Host ""
Write-Host "El emparejamiento queda guardado. En proximas sesiones basta con que"
Write-Host "'Depuracion inalambrica' este ON; si el puerto cambia, actualiza la direccion."
