@echo off
REM ============================================================
REM  LocalPanel - emparejar el movil por Wi-Fi (sin cable).
REM  Doble clic. Ejecuta antes build.bat una vez.
REM ============================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0pair.ps1"
echo.
pause
