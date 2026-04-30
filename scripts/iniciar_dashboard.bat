@echo off
setlocal EnableExtensions

cd /d "%~dp0..\src\DashboardCompras"
if errorlevel 1 goto :fail

set "CONFIG_FILE="
if exist "appsettings.Production.json" set "CONFIG_FILE=appsettings.Production.json"
if not defined CONFIG_FILE if exist "appsettings.json" set "CONFIG_FILE=appsettings.json"

set "PUERTO=5055"
if defined CONFIG_FILE (
  for /f "usebackq delims=" %%P in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "(Get-Content '%CONFIG_FILE%' -Raw | ConvertFrom-Json).ServidorWeb.Puerto" 2^>nul`) do (
    if not "%%P"=="" set "PUERTO=%%P"
  )
)

echo ===============================================
echo AlfaCore - Alfa Gestion
echo Carpeta de trabajo: %CD%
if defined CONFIG_FILE (
  echo Configuracion detectada: %CONFIG_FILE%
) else (
  echo Configuracion detectada: no se encontro appsettings, se usaran valores por defecto.
)
echo URL local esperada: http://localhost:%PUERTO%
echo Si la app escucha en LAN, otras PCs podran entrar por:
echo http://NOMBRE-PC:%PUERTO%
echo ===============================================
echo Cerrando instancia anterior si existe...
for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$exe = Join-Path (Get-Location) 'bin\\Release\\net8.0\\AlfaCore.exe'; $p = Get-Process AlfaCore -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe } | Select-Object -ExpandProperty Id; foreach ($id in $p) { Write-Output $id }" 2^>nul`) do (
  echo Deteniendo AlfaCore PID %%I...
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Stop-Process -Id %%I -Force"
)
echo Iniciando servidor...
echo.

dotnet run --configuration Release --no-launch-profile
set "EXITCODE=%ERRORLEVEL%"

if "%EXITCODE%"=="0" goto :end

echo.
echo ===============================================
echo La aplicacion finalizo con error. Codigo: %EXITCODE%
echo Revisa el mensaje anterior para ver la causa.
echo ===============================================
pause
goto :end

:fail
echo.
echo ===============================================
echo No se pudo abrir la carpeta del proyecto.
echo Ruta esperada: %~dp0..\src\DashboardCompras
echo ===============================================
pause

:end
endlocal
