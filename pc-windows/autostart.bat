@echo off
REM ============================================================
REM  LocalPanel - crear acceso directo + autoarranque (sin admin).
REM  Doble clic.
REM ============================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0autostart.ps1"
echo.
pause
