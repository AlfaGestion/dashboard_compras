@echo off
setlocal
cd /d "%~dp0.."
set "PUBLISH_DIR=.\publish\AlfaCoreLAN"
for %%I in ("%PUBLISH_DIR%") do set "PUBLISH_DIR_FULL=%%~fI"
echo Preparando carpeta de publicacion...
if exist "%PUBLISH_DIR%" (
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -LiteralPath '%PUBLISH_DIR_FULL%' -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue"
)
echo Limpiando residuos de publicacion anterior...
if exist ".\src\AlfaCore\obj" (
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Remove-Item -Recurse -Force '.\src\AlfaCore\obj' -ErrorAction SilentlyContinue"
)
if exist ".\src\AlfaCoreShell\obj" (
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Remove-Item -Recurse -Force '.\src\AlfaCoreShell\obj' -ErrorAction SilentlyContinue"
)
echo Cerrando instancia anterior si existe...
for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$publishDir = (Resolve-Path '%PUBLISH_DIR_FULL%' -ErrorAction SilentlyContinue).Path; $releaseDir = (Resolve-Path '.\src\AlfaCore\bin\Release\net8.0' -ErrorAction SilentlyContinue).Path; $targets = @(); if ($publishDir) { $targets += (Join-Path $publishDir 'AlfaCore.exe') }; if ($releaseDir) { $targets += (Join-Path $releaseDir 'AlfaCore.exe') }; $p = Get-Process AlfaCore -ErrorAction SilentlyContinue | Where-Object { $_.Path -and ($targets -contains $_.Path) } | Select-Object -ExpandProperty Id; foreach ($id in $p) { Write-Output $id }" 2^>nul`) do (
  echo Deteniendo proceso backend PID %%I...
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Stop-Process -Id %%I -Force"
)
for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$publishDir = (Resolve-Path '%PUBLISH_DIR_FULL%' -ErrorAction SilentlyContinue).Path; if (-not $publishDir) { exit 0 }; $shellExe = Join-Path $publishDir 'AlfaCoreShell.exe'; $p = Get-Process AlfaCoreShell -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $shellExe } | Select-Object -ExpandProperty Id; foreach ($id in $p) { Write-Output $id }" 2^>nul`) do (
  echo Deteniendo proceso shell PID %%I...
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Stop-Process -Id %%I -Force"
)
echo Publicando release en %PUBLISH_DIR% ...
dotnet publish .\src\AlfaCore\AlfaCore.csproj -c Release -o %PUBLISH_DIR%
if errorlevel 1 goto :error
dotnet publish .\src\AlfaCoreShell\AlfaCoreShell.csproj -c Release -o %PUBLISH_DIR%
if errorlevel 1 goto :error

echo Copiando documentacion y scripts de servidor...
copy /Y .\README_INSTALACION.md %PUBLISH_DIR%\README_INSTALACION.md >nul
copy /Y .\src\AlfaCore\appsettings.Server.sample.json %PUBLISH_DIR%\appsettings.Server.sample.json >nul
copy /Y .\scripts\Abrir-Firewall.ps1 %PUBLISH_DIR%\Abrir-Firewall.ps1 >nul

(
echo @echo off
echo setlocal
echo cd /d "%%~dp0"
echo set "CONFIG_FILE="
echo if exist "appsettings.Production.json" set "CONFIG_FILE=appsettings.Production.json"
echo if not defined CONFIG_FILE if exist "appsettings.json" set "CONFIG_FILE=appsettings.json"
echo set "PUERTO=5055"
echo if defined CONFIG_FILE ^(
echo   for /f "usebackq delims=" %%%%P in ^(`powershell -NoProfile -Command "$cfg = Get-Content '%%CD%%\%%CONFIG_FILE%%' -Raw ^| ConvertFrom-Json; if ($cfg.ServidorWeb.Puerto) { $cfg.ServidorWeb.Puerto } else { 5055 }"`^) do set "PUERTO=%%%%P"
echo ^)
echo echo ===============================================
echo echo AlfaCore - Alfa Gestion
echo echo Carpeta de trabajo: %%CD%%
echo if defined CONFIG_FILE ^(
echo   echo Configuracion detectada: %%CONFIG_FILE%%
echo ^) else ^(
echo   echo Configuracion detectada: no se encontro appsettings, se usaran valores por defecto.
echo ^)
echo echo URL local esperada: http://localhost:%%PUERTO%%
echo echo Si la app escucha en LAN, otras PCs podran entrar por:
echo echo http://NOMBRE-PC:%%PUERTO%%
echo echo ===============================================
echo echo Iniciando servidor...
echo AlfaCore.exe
echo endlocal
) > %PUBLISH_DIR%\iniciar_dashboard.bat

(
echo @echo off
echo setlocal
echo cd /d "%%~dp0"
echo AlfaCoreShell.exe %%*
echo endlocal
) > %PUBLISH_DIR%\abrir_dashboard_shell.bat

(
echo @echo off
echo setlocal
echo powershell -ExecutionPolicy Bypass -File "%%~dp0Abrir-Firewall.ps1"
echo endlocal
) > %PUBLISH_DIR%\abrir_firewall.bat

echo Publicacion finalizada.
echo Carpeta lista para copiar: %PUBLISH_DIR%
goto :eof

:error
echo La publicacion fallo.
exit /b 1
endlocal
