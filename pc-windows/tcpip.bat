@echo off
REM ============================================================
REM  LocalPanel - activar ADB por red con un cable (una vez).
REM  Para cuando la Depuracion inalambrica se bloquea sin Wi-Fi.
REM  Doble clic. Ejecuta antes build.bat (o copia scrcpy en tools).
REM ============================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tcpip.ps1"
echo.
pause
