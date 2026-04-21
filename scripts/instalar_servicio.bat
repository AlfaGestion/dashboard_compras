@echo off
setlocal

set "SERVICE_NAME=DashboardCompras"
set "DISPLAY_NAME=Dashboard de Compras - Alfa Gestion"
set "DESCRIPTION=Servidor web del Dashboard de Compras. Se inicia automaticamente con Windows."
set "EXE_PATH=%~dp0DashboardCompras.exe"
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
echo Dashboard de Compras - Instalar servicio Windows
echo ================================================

sc query "%SERVICE_NAME%" >nul 2>&1
if not errorlevel 1 (
    echo Deteniendo servicio existente...
    sc stop "%SERVICE_NAME%" >nul 2>&1
    timeout /t 3 /nobreak >nul
    echo Eliminando servicio existente...
    sc delete "%SERVICE_NAME%" >nul 2>&1
    timeout /t 2 /nobreak >nul
)

echo Creando servicio...
sc create "%SERVICE_NAME%" binPath= "\"%EXE_PATH%\"" start= auto DisplayName= "%DISPLAY_NAME%"
if errorlevel 1 (
    echo ERROR: No se pudo crear el servicio.
    if not defined SILENT pause
    exit /b 1
)

sc description "%SERVICE_NAME%" "%DESCRIPTION%" >nul 2>&1
sc failure "%SERVICE_NAME%" reset= 86400 actions= restart/5000/restart/10000/restart/30000 >nul 2>&1

echo Iniciando servicio...
sc start "%SERVICE_NAME%" >nul 2>&1
if errorlevel 1 (
    echo ADVERTENCIA: El servicio se creo pero no pudo iniciarse ahora.
    echo Podra iniciarlo desde services.msc o reiniciando el servidor.
    if not defined SILENT pause
    exit /b 0
)

echo ================================================
echo Servicio instalado e iniciado correctamente.
echo Para gestionar: services.msc
echo ================================================
if not defined SILENT pause
