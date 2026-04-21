# Dashboard de Compras - Alfa Gestión

## Qué ya queda resuelto
- La app corre con Kestrel sin IIS.
- Escucha en LAN por el puerto configurado.
- Abre el navegador automáticamente en la PC servidor.
- Muestra en logs la URL local y las URLs de acceso por nombre/IP.
- La cadena SQL Server y el puerto quedan configurables por `appsettings.json`.
- La publicación deja una carpeta final lista para copiar.
- Ahora también se puede generar un instalador `Setup.exe` para distribuir desde web o por enlace.

## Instalador para distribuir por web
Si querés subir un instalador a tu web para que el cliente lo descargue e instale en la PC servidor:

1. Ejecutá:
   - `scripts\publicar_instalador.bat`
2. El script hace esto:
   - publica la app en limpio
   - arma una carpeta de entrada para setup
   - si detecta Inno Setup 6, compila el instalador
3. La salida queda en:
   - `publish\DashboardComprasInstaller\Output`

Notas:
- Si Inno Setup no está instalado, igual queda preparada la carpeta:
  - `publish\DashboardComprasInstaller\Input`
- El archivo del instalador queda definido en:
  - `installer\DashboardComprasServidor.iss`
- Después solo hay que abrir ese `.iss` con Inno Setup y compilar.

Resultado esperado:
- un archivo tipo:
  - `DashboardComprasSetup_1.0.0.exe`
- ese es el archivo que podés subir a tu web para descarga.

## Publicación recomendada
1. Ejecutá:
   - `scripts\publicar_release.bat`
2. Se genera la carpeta:
   - `publish\DashboardComprasLAN`
3. Esa carpeta ya incluye:
   - `DashboardCompras.exe`
   - `DashboardComprasShell.exe`
   - `iniciar_dashboard.bat`
   - `abrir_dashboard_shell.bat`
   - `abrir_firewall.bat`
   - `Abrir-Firewall.ps1`
   - `README_INSTALACION.md`
   - `appsettings.Server.sample.json`

## Instalación en PC servidor
1. Copiá la carpeta `publish\DashboardComprasLAN` a la PC servidor.
2. Si querés partir de una plantilla limpia, abrí `appsettings.Server.sample.json` y copiá sus valores a `appsettings.Production.json`.
3. Editá `appsettings.Production.json`.
4. Configurá:
   - `ConnectionStrings:AlfaGestion`
   - `ServidorWeb:Puerto`
   - opcionalmente `ServidorWeb:UrlBasePublica`
5. Si querés mantener todo simple, dejá `ServidorWeb:EscucharEnRed` en `true`.
6. Ejecutá `abrir_firewall.bat` como administrador.
7. Ejecutá `iniciar_dashboard.bat`.
8. Verificá acceso local:
   - `http://localhost:5055`

## Uso con shell de escritorio
- `DashboardComprasShell.exe`
  Abre el dashboard en una ventana de escritorio con `WebView2`.
- `abrir_dashboard_shell.bat`
  Atajo simple para abrir el shell.

Ejemplos:
- Local con backend incluido:
  - `DashboardComprasShell.exe`
- Remoto contra servidor:
  - `DashboardComprasShell.exe --server=NOMBRE-PC --port=5055`
- URL directa:
  - `DashboardComprasShell.exe --url=http://NOMBRE-PC:5055`

## Separación de configuración
- `appsettings.json`
  Configuración base general.
- `appsettings.Production.json`
  Configuración recomendada para la PC servidor cuando ejecutás `DashboardCompras.exe`.
- `appsettings.Server.sample.json`
  Plantilla para armar una configuración nueva sin tocar el entorno de desarrollo.

Sugerencia práctica:
- Desarrollo: mantener `appsettings.json`.
- Servidor: dejar valores finales en `appsettings.Production.json`.
- Si cambiás el puerto, abrí nuevamente el firewall para ese nuevo puerto.

## Acceso desde otras PCs
Desde cualquier PC cliente de la red:
- Abrí un navegador.
- Ingresá por nombre de equipo:
  - `http://NOMBRE-PC:5055`
- O por IP:
  - `http://192.168.1.50:5055`

Sugerencia práctica:
- Crear un acceso directo en cada cliente apuntando a:
  - `http://NOMBRE-PC:5055`

## Scripts incluidos
- `scripts\publicar_release.bat`
  Publica la app y arma la carpeta final.
- `scripts\publicar_instalador.bat`
  Publica la app, prepara la carpeta del setup y compila el instalador si Inno Setup está instalado.
- `publish\DashboardComprasLAN\iniciar_dashboard.bat`
  Inicia la app publicada usando el `.exe`.
- `publish\DashboardComprasLAN\abrir_dashboard_shell.bat`
  Abre el dashboard en modo escritorio usando `WebView2`.
- `publish\DashboardComprasLAN\abrir_firewall.bat`
  Abre el puerto configurado en Firewall de Windows.
- `publish\DashboardComprasLAN\Abrir-Firewall.ps1`
  Script PowerShell usado por el `.bat`. Toma el puerto desde `appsettings.Production.json` o `appsettings.json`.

## Flujo recomendado de uso real
1. Publicar con `scripts\publicar_release.bat`.
2. Copiar `publish\DashboardComprasLAN` a la PC servidor.
3. Editar `appsettings.Production.json`.
4. Ejecutar `abrir_firewall.bat` como administrador.
5. Ejecutar `iniciar_dashboard.bat`.
6. Desde otra PC entrar por:
   - `http://NOMBRE-PC:PUERTO`
   - `http://IP:PUERTO`

## Flujo recomendado si lo vas a distribuir desde web
1. Ejecutar `scripts\publicar_instalador.bat`.
2. Tomar el archivo `Setup.exe` de:
   - `publish\DashboardComprasInstaller\Output`
3. Subir ese archivo a tu web o compartirlo por enlace.
4. El cliente lo descarga en la PC servidor.
5. Instala el dashboard.
6. Abre `Manual de instalacion` o ejecuta `Iniciar Dashboard`.
7. Desde otras PCs entra por navegador.

## Solución de problemas
### Puerto ocupado
- Cambiá `ServidorWeb:Puerto` en `appsettings.json` o `appsettings.Production.json`.
- Volvé a iniciar la app.

### Firewall bloqueando
- Ejecutá `abrir_firewall.bat` como administrador.
- Verificá que el puerto configurado coincida con el de la app.
- Si cambiaste el puerto después de abrir el firewall, ejecutá otra vez el script.

### No resuelve el nombre de la PC
- Probá con la IP:
  - `http://IP:PUERTO`
- Revisá conectividad de red entre equipos.

### Falla SQL Server
- Revisá servidor, base, usuario y clave en `ConnectionStrings:AlfaGestion`.
- Confirmá que la PC servidor tenga acceso al SQL Server.
- La app mostrará mensajes de error y el log conservará la excepción.

### El navegador local abre pero otra PC no entra
- Confirmá que `ServidorWeb:EscucharEnRed` esté en `true`.
- Confirmá que el puerto del firewall coincida con `ServidorWeb:Puerto`.
- Probá por IP si el nombre de la PC no resuelve.

## Ejemplo de configuración
```json
"ConnectionStrings": {
  "AlfaGestion": "Server=AGSERVER\\ALFANET;Database=ALFANET;User ID=ALFANET;Password=ALFANET;TrustServerCertificate=True;"
},
"ServidorWeb": {
  "Puerto": 5055,
  "EscucharEnRed": true,
  "AbrirNavegadorAlIniciar": true,
  "Protocolo": "http",
  "UrlBasePublica": "http://NOMBRE-PC:5055"
}
```
