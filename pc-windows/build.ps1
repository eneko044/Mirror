# ============================================================
#  LocalPanel - build (SIN administrador)
#  1) Instala/detecta .NET 8 SDK en tu usuario (sin admin)
#  2) Compila LocalPanel.exe (autocontenido, un archivo)
#  3) Descarga scrcpy + adb en publish\tools
# ============================================================
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Set-Location -Path $PSScriptRoot

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  LocalPanel - compilar y preparar"          -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# --- 1) .NET 8 SDK (instalacion en el usuario, sin admin) ---
$dotnet = "dotnet"
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    $userDotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
    if (Test-Path $userDotnet) {
        $dotnet = $userDotnet
    } else {
        Write-Host "[1/3] No hay .NET SDK. Lo instalo en tu usuario (sin admin)..."
        $script = Join-Path $env:TEMP "dotnet-install.ps1"
        Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $script
        & $script -Channel 8.0 -InstallDir (Join-Path $env:USERPROFILE ".dotnet")
        $dotnet = $userDotnet
    }
}
if ($dotnet -ne "dotnet" -and -not (Test-Path $dotnet)) {
    Write-Host "No se pudo preparar .NET SDK." -ForegroundColor Red
    Write-Host "Instala .NET 8 SDK (sirve la version 'user'): https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}
Write-Host "[1/3] .NET SDK listo." -ForegroundColor Green

# --- 2) Compilar el exe ---
Write-Host "[2/3] Compilando LocalPanel.exe ..."
& $dotnet publish -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR al compilar. Revisa el mensaje de arriba." -ForegroundColor Red
    exit 1
}

# --- 3) scrcpy + adb en publish\tools ---
$toolsDir = Join-Path $PSScriptRoot "publish\tools"
if (Test-Path (Join-Path $toolsDir "scrcpy.exe")) {
    Write-Host "[3/3] scrcpy ya presente, salto la descarga." -ForegroundColor Green
} else {
    Write-Host "[3/3] Descargando scrcpy (incluye adb) ..."
    try {
        $h = @{ 'User-Agent' = 'LocalPanel' }
        $rel = Invoke-RestMethod -Uri 'https://api.github.com/repos/Genymobile/scrcpy/releases/latest' -Headers $h
        $asset = $rel.assets | Where-Object { $_.name -match 'scrcpy-win64-.*\.zip' } | Select-Object -First 1
        if (-not $asset) { throw "No encuentro el zip win64 de scrcpy" }
        $zip = Join-Path $env:TEMP "scrcpy.zip"
        $tmp = Join-Path $env:TEMP "scrcpy_tmp"
        Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip -Headers $h
        if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
        Expand-Archive -Path $zip -DestinationPath $tmp -Force
        $src = $tmp
        if (-not (Test-Path (Join-Path $src 'scrcpy.exe'))) {
            $sub = Get-ChildItem -Directory $src | Select-Object -First 1
            if ($sub) { $src = $sub.FullName }
        }
        New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null
        Copy-Item -Path (Join-Path $src '*') -Destination $toolsDir -Recurse -Force
        Remove-Item $zip, $tmp -Recurse -Force
        Write-Host "[3/3] scrcpy listo en publish\tools." -ForegroundColor Green
    } catch {
        Write-Host "No se pudo descargar scrcpy automaticamente: $_" -ForegroundColor Yellow
        Write-Host "Descargalo de https://github.com/Genymobile/scrcpy/releases"
        Write-Host "y copia TODO su contenido en: $toolsDir"
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  LISTO" -ForegroundColor Green
Write-Host ("  Ejecutable: " + (Join-Path $PSScriptRoot "publish\LocalPanel.exe"))
Write-Host "  (Puedes renombrarlo; el proceso hereda ese nombre.)"
Write-Host "============================================" -ForegroundColor Green
