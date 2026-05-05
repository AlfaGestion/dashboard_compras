@echo off
setlocal

set "SERVICE_NAME=AlfaCore"
set "LEGACY_SERVICE_NAME=AlfaCore"
set "SILENT="
if /i "%~1"=="/silent" set "SILENT=1"

net session >nul 2>&1
if errorlevel 1 (
    echo Este script requiere permisos de administrador.
    echo Ejecutalo con clic derecho ^> Ejecutar como administrador.
    if not defined SILENT pause
    exit /b 1
)

echo ================================================
echo AlfaCore - Desinstalar servicio
echo ================================================

if /i not "%LEGACY_SERVICE_NAME%"=="%SERVICE_NAME%" (
    sc query "%LEGACY_SERVICE_NAME%" >nul 2>&1
    if not errorlevel 1 (
        echo Eliminando servicio anterior %LEGACY_SERVICE_NAME%...
        sc stop "%LEGACY_SERVICE_NAME%" >nul 2>&1
        timeout /t 4 /nobreak >nul
        sc delete "%LEGACY_SERVICE_NAME%" >nul 2>&1
        timeout /t 2 /nobreak >nul
    )
)

sc query "%SERVICE_NAME%" >nul 2>&1
if errorlevel 1 (
    echo El servicio no esta instalado.
    if not defined SILENT pause
    exit /b 0
)

echo Deteniendo servicio...
sc stop "%SERVICE_NAME%" >nul 2>&1
timeout /t 4 /nobreak >nul

echo Eliminando servicio...
sc delete "%SERVICE_NAME%"
if errorlevel 1 (
    echo ERROR: No se pudo eliminar el servicio.
    if not defined SILENT pause
    exit /b 1
)

echo ================================================
echo Servicio eliminado correctamente.
echo ================================================
if not defined SILENT pause
