#define MyAppName "Dashboard de Compras - Alfa Gestion"
#define MyAppPublisher "Alfa Gestion"
#define MyAppExeName "DashboardCompras.exe"
#define MyAppLauncher "iniciar_dashboard.bat"
#define MyAppShellLauncher "abrir_dashboard_shell.bat"
#define MyReadmeName "README_INSTALACION.md"

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#ifndef SourceDir
  #define SourceDir "..\publish\DashboardComprasInstaller\Input"
#endif

#ifndef OutputDir
  #define OutputDir "..\publish\DashboardComprasInstaller\Output"
#endif

[Setup]
AppId={{A3C27933-8FEA-4D63-90D0-1F334CEFC4B1}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Alfa Gestion\Dashboard de Compras
DefaultGroupName=Alfa Gestion\Dashboard de Compras
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=DashboardComprasSetup_{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupLogging=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: unchecked
Name: "openreadme"; Description: "Abrir manual de instalacion al finalizar"; GroupDescription: "Acciones sugeridas:"; Flags: checkedonce
Name: "openfirewall"; Description: "Abrir el puerto en Firewall de Windows al finalizar"; GroupDescription: "Acciones sugeridas:"; Flags: unchecked
Name: "launchapp"; Description: "Iniciar Dashboard de Compras al finalizar"; GroupDescription: "Acciones sugeridas:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "appsettings.Development.json,*.pdb,*.log,prereqs\*"
#ifdef IncludeHostingBundle
Source: "{#SourceDir}\prereqs\dotnet-hosting-win.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsHostingBundle
#endif

[Icons]
Name: "{autoprograms}\Alfa Gestion\Dashboard de Compras\Iniciar Dashboard"; Filename: "{app}\{#MyAppLauncher}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autoprograms}\Alfa Gestion\Dashboard de Compras\Abrir Dashboard Shell"; Filename: "{app}\{#MyAppShellLauncher}"; WorkingDir: "{app}"; IconFilename: "{app}\DashboardComprasShell.exe"
Name: "{autoprograms}\Alfa Gestion\Dashboard de Compras\Manual de instalacion"; Filename: "{app}\{#MyReadmeName}"
Name: "{autodesktop}\Dashboard de Compras"; Filename: "{app}\{#MyAppLauncher}"; WorkingDir: "{app}"; Tasks: desktopicon; IconFilename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyReadmeName}"; Description: "Abrir manual de instalacion"; Flags: postinstall shellexec skipifsilent; Tasks: openreadme
Filename: "{cmd}"; Parameters: "/c ""{app}\abrir_firewall.bat"""; Description: "Abrir el puerto en Firewall de Windows"; Flags: postinstall runhidden waituntilterminated skipifsilent; Tasks: openfirewall
Filename: "{cmd}"; Parameters: "/c ""{app}\{#MyAppLauncher}"""; Description: "Iniciar Dashboard de Compras"; Flags: postinstall skipifsilent; Tasks: launchapp

[UninstallDelete]
Type: files; Name: "{app}\shell_backend_stdout.log"
Type: files; Name: "{app}\shell_backend_stderr.log"
Type: files; Name: "{app}\backend_startup.log"

[Code]
function IsNetRuntimeInstalled(RuntimeId: string): Boolean;
var
  SubkeyNames: TArrayOfString;
  KeyPath: string;
  I: Integer;
begin
  Result := False;
  KeyPath := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\' + RuntimeId;
  if RegGetSubkeyNames(HKLM64, KeyPath, SubkeyNames) then
    for I := 0 to GetArrayLength(SubkeyNames) - 1 do
      if Copy(SubkeyNames[I], 1, 2) = '8.' then
      begin
        Result := True;
        Exit;
      end;
end;

function NeedsHostingBundle(): Boolean;
begin
  Result := not IsNetRuntimeInstalled('Microsoft.AspNetCore.App');
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
  BundlePath: string;
begin
  Result := '';
  if not NeedsHostingBundle() then
    Exit;

  BundlePath := ExpandConstant('{tmp}\dotnet-hosting-win.exe');

  if not FileExists(BundlePath) then
  begin
    MsgBox(
      'No se encontro el instalador de .NET 8 Hosting Bundle.' + #13#10#13#10 +
      'Instalelo manualmente antes de usar la aplicacion:' + #13#10 +
      'https://dotnet.microsoft.com/es-es/download/dotnet/8.0' + #13#10 +
      '(descargar "Hosting Bundle" para Windows)',
      mbInformation, MB_OK);
    Exit;
  end;

  MsgBox(
    'Se instalara el .NET 8 Hosting Bundle.' + #13#10 +
    'Este proceso puede tardar unos minutos. Haga clic en Aceptar para continuar.',
    mbInformation, MB_OK);

  if ShellExec('runas', BundlePath, '/passive /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    if ResultCode = 3010 then
      NeedsRestart := True
    else if ResultCode <> 0 then
      MsgBox(
        'La instalacion de .NET 8 Hosting Bundle termino con codigo: ' + IntToStr(ResultCode) + #13#10 +
        'Si la aplicacion no inicia, instalelo manualmente desde:' + #13#10 +
        'https://dotnet.microsoft.com/es-es/download/dotnet/8.0',
        mbInformation, MB_OK);
  end
  else
    MsgBox(
      'No se pudo ejecutar el instalador de .NET 8 Hosting Bundle.' + #13#10#13#10 +
      'Instalelo manualmente antes de usar la aplicacion:' + #13#10 +
      'https://dotnet.microsoft.com/es-es/download/dotnet/8.0' + #13#10 +
      '(descargar "Hosting Bundle" para Windows)',
      mbInformation, MB_OK);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  #ifndef IncludeHostingBundle
  if NeedsHostingBundle() then
  begin
    if MsgBox(
      'Falta el componente requerido: ASP.NET Core 8 Runtime.' + #13#10#13#10 +
      'Instale el ".NET 8 Hosting Bundle" y vuelva a ejecutar el instalador.' + #13#10#13#10 +
      '¿Desea abrir la pagina de descarga ahora?',
      mbConfirmation, MB_YESNO) = IDYES then
      ShellExec('open', 'https://dotnet.microsoft.com/es-es/download/dotnet/8.0', '', '', SW_SHOW, ewNoWait, ErrorCode);
    Result := False;
  end;
  #endif
end;

