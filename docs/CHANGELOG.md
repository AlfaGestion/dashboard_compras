# Changelog — Dashboard de Compras - Alfa Gestión

Formato: `[versión] — fecha — descripción`

---

## [1.2.0] — 2026-04-22

### Agregado

**Módulo Diseñador de Consultas** (`/consultas`):
- Árbol jerárquico organizado por campo CLAVE (2 dígitos por nivel, hasta 6 niveles). Nodos expandibles/colapsables, buscador en tiempo real por nombre o código.
- Ejecución con parámetros tipados (fecha, número, texto). Sin parámetros se ejecuta automáticamente al abrir.
- Resultado interactivo: buscador sobre todas las columnas (Enter/Esc), ordenamiento por encabezado (asc/desc/original), paginación configurable 25/50/100/200 filas.
- **Gráficos SVG** sin dependencias JS: barras agrupadas y líneas Catmull-Rom. Ejes configurables, preselección de columnas desde metadato `CamposTotaliza`, límite 60 puntos.
- **Tabla dinámica de agrupación**: columna agrupadora seleccionable, operación por columna (Suma / Conteo / Promedio / Mínimo / Máximo / excluir), fila de totales globales, ordenamiento numérico. Se recalcula automáticamente al aplicar filtro.
- Editor: campos básicos + metadatos opcionales (tabla, campos a totalizar, orden por defecto). Validación de instrucciones de escritura. Token `<P>` para parámetros en tiempo de ejecución.
- Constructor visual: búsqueda de tablas/vistas en `INFORMATION_SCHEMA`, selector de columnas con resaltado de búsqueda, filtros con soporte `<P>`, ORDER BY configurable. SQL generado es editable.
- Duplicar consulta con pre-llenado completo.
- Exportación a Excel sin límite de filas (ClosedXML tabla formateada) y PDF por impresión del navegador.
- **Gestor de sesiones**: panel lateral para cambiar conexión activa, alta/baja de sesiones, persistencia en `App_Data/sessions.json`. Árbol se recarga al cambiar sesión.

**Ayuda contextual por módulo**: `/ayuda?topic=consultas` carga `manual_consultas.md`; sin parámetro carga `manual_usuario.md`. Sidebar y botón `?` del header apuntan al manual del módulo activo.

**Botón PDF condicional**: visible solo en páginas del módulo `compras`; oculto en Consultas, Ayuda e Inicio.

---

## [1.1.0] — 2026-04-21

### Agregado
- **Launcher de aplicaciones** (`/`): página principal con grilla de módulos estilo Odoo. Compras activo, Ventas / Stock / Caja y Bancos / Contabilidad / Diseñador de consultas con badge "Próximamente".
- **Estructura de rutas por módulo**: todas las páginas de Compras ahora bajo `/compras/*` (ej. `/compras/proveedores`, `/compras/comprobantes`).
- **Sidebar context-aware**: muestra la navegación del módulo activo. Incluye botón "← Aplicaciones" para volver al launcher.
- **Logo de Alfa Gestión** en el sidebar (reemplaza el texto "AG").
- **Modo claro / oscuro**: toggle con ícono luna/sol en el header. Persistido en localStorage. Default: modo claro. Sin flash al cargar.
- **InformesIA** (`/compras/informesia`): consultas en lenguaje natural, dictado por voz, sugerencias rápidas, historial, resultado en nueva pestaña con URL propia.
- **Servicio de Windows**: `instalar_servicio.bat` y `desinstalar_servicio.bat`. El instalador lo instala automáticamente con reinicio automático ante fallas.
- **Installer con .NET 8 Hosting Bundle**: se descarga y cachea en `installer\prereqs\`. Se instala automáticamente durante el setup si falta el runtime.
- **Primera configuración de conexión**: si `appsettings.Production.json` no tiene credenciales, la app las solicita por consola al primer inicio y las guarda.
- **Gráfico LineChart mejorado**: curvas Catmull-Rom (suaves), etiquetas X posicionadas en SVG, máximo 8 etiquetas visibles, puntos solo cuando hay ≤ 20 datos.

### Cambiado
- `DashboardComprasShell` eliminó WebView2: ahora abre el navegador por defecto con `Process.Start` y se cierra.
- Installer limpia credenciales de conexión antes de empaquetar (`appsettings.json` con string vacío, elimina `appsettings.Production.json`).
- `GetCurrentPageTitle()` en MainLayout actualizado para rutas con prefijo `/compras/`.
- Sidebar colapsable: la etiqueta "Próximamente" se oculta al colapsar.
- `BackendLauncher` redirige stdout/stderr a `backend_startup.log` para diagnóstico de errores de inicio.

### Corregido
- Flag `checked` inválido en tarea de Inno Setup → cambiado a `checkedonce`.
- Variable `ErrorCode` no declarada en `InitializeSetup()` de Inno Setup.
- Bloqueo de instalación cuando .NET SDK está instalado pero el registro del runtime difiere del Hosting Bundle: el check ahora es no bloqueante (avisa pero permite continuar).
- Etiquetas SVG `<text>` con atributos en Blazor Razor → resuelto con `MarkupString`.

---

## [1.0.0] — 2026-04-20

### Agregado
- **Módulo Compras completo**:
  - Inicio con KPIs ejecutivos, evolución mensual, top proveedores y rubros
  - Proveedores con ranking, concentración, variación y ficha con drill down
  - Comprobantes con paginación, filtros rápidos, alertas automáticas y modal de detalle
  - Rubros con análisis de concentración, crecimiento/caída y ficha
  - Familias con estructura jerárquica, concentración y ficha
  - Artículos con variación de precio, proveedor principal y ficha
  - Actividad operativa por usuario con series diarias y ficha
- **Filtros globales persistentes** (`FilterStateService`): fecha, proveedor, artículo, rubro, familia, usuario, sucursal, depósito, estado, tipo comprobante
- **Accesos rápidos de fecha**: Hoy, Esta semana, Este mes, Mes anterior, Últimos 3 meses, Año actual
- **Componentes SVG propios**: `LineChart`, `HorizontalBarChart` (sin dependencias externas)
- **Componentes reutilizables**: `KpiCard`, `DataTable`, `DetailCard`, `GlobalFiltersBar`
- **Exportar PDF** de cualquier página (`window.print()` con CSS de impresión)
- **Sidebar colapsable** con estado persistido en localStorage
- **Insights automáticos** en Rubros, Familias y Artículos
- **Alertas operativas** en Comprobantes (importe cero, sin detalle, IVA cero)
- **Vistas SQL**: `vw_compras_cabecera_dashboard`, `vw_compras_detalle_dashboard`
- **Instalador Inno Setup** (`DashboardComprasServidor.iss`) con shortcuts, firewall, readme
- **Scripts de publicación**: `publicar_release.bat`, `publicar_instalador.bat`
- **Ayuda / Manual de usuario** con índice, búsqueda y anclas por sección
