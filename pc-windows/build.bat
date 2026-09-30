@echo off
REM ============================================================
REM  LocalPanel - compila el .exe y prepara scrcpy+adb.
REM  Doble clic. NO requiere administrador.
REM  (Toda la logica esta en build.ps1, al lado de este archivo.)
REM ============================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
echo.
pause
