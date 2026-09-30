# ============================================================
#  LocalPanel - crear acceso directo y autoarranque (SIN admin)
#  Crea un icono en el Escritorio y hace que se abra al iniciar sesion.
# ============================================================
$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

$exe = Get-ChildItem $PSScriptRoot -Recurse -Filter LocalPanel.exe -ErrorAction SilentlyContinue |
       Select-Object -First 1
if (-not $exe) {
    Write-Host "No encuentro LocalPanel.exe. Ejecuta build.bat primero." -ForegroundColor Red
    exit 1
}
$target  = $exe.FullName
$workdir = Split-Path $target
$ws = New-Object -ComObject WScript.Shell

# 1) Acceso directo en el Escritorio
$desktop = [Environment]::GetFolderPath('Desktop')
$s1 = $ws.CreateShortcut((Join-Path $desktop 'LocalPanel.lnk'))
$s1.TargetPath = $target
$s1.WorkingDirectory = $workdir
$s1.Save()

# 2) Autoarranque al iniciar sesion (carpeta Inicio del usuario)
$startup = [Environment]::GetFolderPath('Startup')
$s2 = $ws.CreateShortcut((Join-Path $startup 'LocalPanel.lnk'))
$s2.TargetPath = $target
$s2.WorkingDirectory = $workdir
$s2.Save()

Write-Host "=== LISTO ===" -ForegroundColor Green
Write-Host " - Icono 'LocalPanel' en el Escritorio (doble clic para abrir)."
Write-Host " - Se abrira solo al iniciar sesion en Windows."
Write-Host ""
Write-Host "Ojo: en modo USB necesita el movil conectado para funcionar;"
Write-Host "si al iniciar sesion no esta conectado, saldra un aviso (dale a Aceptar)."
Write-Host ""
Write-Host "Para QUITAR el autoarranque: Win+R -> escribe  shell:startup  -> borra 'LocalPanel'."
