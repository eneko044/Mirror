@echo off
REM ============================================================
REM  LocalPanel - compila el .exe y prepara scrcpy+adb.
REM  Ejecutalo con doble clic. No requiere administrador.
REM ============================================================
cd /d "%~dp0"
title LocalPanel - build

echo ============================================
echo   LocalPanel - compilar y preparar
echo ============================================
echo.

REM --- 1) .NET 8 SDK ---
where dotnet >nul 2>&1
if errorlevel 1 goto no_dotnet
goto have_dotnet

:no_dotnet
echo No se encontro .NET SDK. Intento instalarlo con winget...
where winget >nul 2>&1
if errorlevel 1 (
  echo.
  echo   No hay winget. Instala .NET 8 SDK a mano:
  echo   https://dotnet.microsoft.com/download/dotnet/8.0
  echo   Luego vuelve a ejecutar este archivo.
  echo.
  pause
  exit /b 1
)
winget install --id Microsoft.DotNet.SDK.8 -e --accept-package-agreements --accept-source-agreements
echo.
echo   .NET instalado. CIERRA esta ventana y vuelve a ejecutar build.bat
echo   (hace falta para que Windows vea el nuevo PATH).
echo.
pause
exit /b 0

:have_dotnet
echo [1/3] .NET SDK detectado.
echo.

REM --- 2) Compilar el exe (autocontenido, un solo archivo) ---
echo [2/3] Compilando LocalPanel.exe ...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish
if errorlevel 1 (
  echo.
  echo   ERROR al compilar. Revisa el mensaje de arriba.
  pause
  exit /b 1
)
echo.

REM --- 3) scrcpy + adb en publish\tools ---
if exist "publish\tools\scrcpy.exe" (
  echo [3/3] scrcpy ya estaba en publish\tools, salto la descarga.
  goto done
)
echo [3/3] Descargando scrcpy (incluye adb) ...
powershell -NoProfile -ExecutionPolicy Bypass -Command "[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12; $ErrorActionPreference='Stop'; $h=@{'User-Agent'='LocalPanel'}; $r=Invoke-RestMethod -Uri 'https://api.github.com/repos/Genymobile/scrcpy/releases/latest' -Headers $h; $a=$r.assets | Where-Object { $_.name -match 'scrcpy-win64-.*\.zip' } | Select-Object -First 1; if(-not $a){throw 'No encuentro el zip win64 de scrcpy'}; Invoke-WebRequest -Uri $a.browser_download_url -OutFile 'scrcpy.zip' -Headers $h; if(Test-Path 'scrcpy_tmp'){Remove-Item 'scrcpy_tmp' -Recurse -Force}; Expand-Archive -Path 'scrcpy.zip' -DestinationPath 'scrcpy_tmp' -Force; $src='scrcpy_tmp'; if(-not (Test-Path (Join-Path $src 'scrcpy.exe'))){ $d=Get-ChildItem -Directory $src | Select-Object -First 1; if($d){$src=$d.FullName} }; New-Item -ItemType Directory -Force -Path 'publish\tools' | Out-Null; Copy-Item -Path (Join-Path $src '*') -Destination 'publish\tools' -Recurse -Force; Remove-Item 'scrcpy.zip','scrcpy_tmp' -Recurse -Force"
if errorlevel 1 (
  echo.
  echo   No se pudo descargar scrcpy automaticamente (sin internet o bloqueado).
  echo   Descargalo de https://github.com/Genymobile/scrcpy/releases
  echo   y copia TODO su contenido en:  %cd%\publish\tools
  echo.
)

:done
echo.
echo ============================================
echo   LISTO
echo   Ejecutable:  %cd%\publish\LocalPanel.exe
echo   (Puedes renombrarlo; el proceso hereda ese nombre.)
echo ============================================
echo.
pause
