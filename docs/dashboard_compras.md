# 🧾 Proyecto: Dashboard de Compras - Alfa Gestión

## 🎯 Objetivo

Desarrollar un dashboard interactivo de compras que permita visualizar, analizar y explorar la información de compras desde el sistema Alfa Gestión.

---

## 🧠 Alcance implementado

* Resumen ejecutivo (Inicio)
* Análisis por proveedor con drill down
* Análisis por rubro y familia con drill down
* Análisis por artículos con evolución de precios
* Consulta y detalle de comprobantes
* Seguimiento de actividad operativa por usuario
* Filtros globales persistentes entre páginas

---

## 🗄️ Origen de datos

### Tablas principales

* `C_MV_Cpte` (Cabecera)
* `C_MV_CpteInsumos` (Detalle)

### Tablas maestras

* `Vt_Proveedores`
* `V_MA_ARTICULOS`
* `V_TA_Rubros`
* `V_TA_FAMILIAS`

---

## 🔄 Tipos de comprobantes

### ➕ Suman (signo +1)

* FCC
* NDC
* LIQC
* FPC

### ➖ Restan (signo -1)

* NCC
* NCPC

---

## ⚙️ Lógica de negocio

### Signo

* +1 → compras (facturas)
* -1 → créditos (notas de crédito)

### Importes calculados (aplicados en las vistas SQL)

* `ImporteDashboard` — importe total con signo
* `NetoDashboard` — neto sin IVA con signo
* `IvaDashboard` — monto IVA con signo
* `CantidadDashboard` — cantidad de ítems con signo
* `TotalDashboard` — importe final para análisis

### Alertas operativas automáticas

* Comprobante con importe = 0
* Comprobante sin detalle de artículos
* Comprobante con IVA = 0 en importe significativo

---

## 🧱 Vistas SQL

### `vw_compras_cabecera_dashboard`

* Una fila por comprobante
* Incluye importes calculados con signo
* Incluye campos: `TC`, `IDCOMPROBANTE`, `NUMERO`, `FECHA`, `CUENTA`, `RAZON_SOCIAL`, `SUCURSAL`, `IdDeposito`, `USUARIO`, `EstadoComprobante`
* Incluye flags derivados: `TieneDetalle`, `EsContable`, `IvaEnCero`, `AlertaOperativa`
* Incluye campos calculados: `ImporteDashboard`, `NetoDashboard`, `IvaDashboard`

### `vw_compras_detalle_dashboard`

* Una fila por ítem de comprobante
* Incluye datos del artículo: `IDARTICULO`, `DESCRIPCION_ARTICULO`, `RUBRO`, `FAMILIA`
* Incluye cantidades y totales con signo: `CantidadDashboard`, `TotalDashboard`
* Hereda campos de cabecera: `TC`, `IDCOMPROBANTE`, `CUENTA`, `FECHA`, `USUARIO`, `SUCURSAL`, `IdDeposito`

---

## 💻 Tecnología

* **Runtime:** .NET 8 / ASP.NET Core
* **UI:** Blazor Server (Interactive Server rendering)
* **Base de datos:** SQL Server (acceso directo con `Microsoft.Data.SqlClient`)
* **Gráficos:** SVG custom (LineChart, HorizontalBarChart — sin dependencia externa)
* **Hosting:** Kestrel standalone (sin IIS)
* **Cultura:** es-AR

---

## 🖥️ Diseño de la app

### Menú (implementado)

* Inicio
* Proveedores
* Comprobantes
* Rubros
* Familias
* Artículos
* Actividad

---

## 📊 Pantallas

### Inicio

**KPIs:**
* Total comprado, Neto total, IVA total
* Cantidad comprobantes, Artículos distintos, Proveedores activos, Ticket promedio

**Gráficos:**
* Evolución mensual (LineChart — 12 meses)
* Top 7 proveedores (HorizontalBarChart)
* Top 7 rubros (HorizontalBarChart)

**Tabla:**
* Top 10 artículos con link a detalle

---

### Proveedores

**KPIs:** `ProveedoresKpiDto`
* Total comprado, proveedores activos, proveedor principal
* Concentración Top 5, variación vs período anterior
* Mayor crecimiento, mayor caída

**Gráficos:**
* Ranking Top 10 proveedores
* Concentración Top 5 vs resto

**Tabla:** `ProveedorResumenDto`
* Cuenta, razón social, total, participación %, comprobantes, ticket promedio, última compra, variación, botón ficha

**Ficha de proveedor:** `ProveedorDetalleDto`
* Top artículos comprados al proveedor
* Últimos comprobantes
* Evolución mensual del gasto

---

### Comprobantes

**KPIs:** `ComprobantesOverviewDto`
* Importe total, cantidad, proveedores activos, ticket promedio
* Comprobantes en cero, sin detalle, IVA cero, máximo comprobante
* TC predominante y su participación

**Gráficos:**
* Evolución semanal (LineChart)
* Composición por tipo de TC (HorizontalBarChart)

**Alertas automáticas:** `ComprobantesAlertDto`
* Detección y resumen de comprobantes problemáticos

**Filtros rápidos:**
* Críticos, con detalle, contables, valor cero, IVA cero, sin detalle

**Tabla paginada (20/página):** `ComprobanteDto`
* Fecha, TC, número, proveedor, cuenta, neto, IVA, total
* Estado, tiene detalle, cantidad ítems, tipo de carga (con detalle / contable)
* Alertas, sucursal, depósito, usuario

**Modal de detalle:** `ComprobanteDetalleDto` + `ComprobanteItemDto`
* Líneas del comprobante: artículo, descripción, rubro, familia, cantidad, costo, total

---

### Rubros

**KPIs:** `RubrosKpiDto`
* Total comprado, rubros activos, rubro principal y su participación
* Variación vs anterior, concentración Top 3
* Mayor crecimiento, mayor caída

**Gráficos:**
* Top rubros (HorizontalBarChart)
* Distribución del gasto (HorizontalBarChart — porcentajes)
* Concentración Top 3 vs resto
* Rubros en crecimiento / en caída

**Insights automáticos:** `RubrosInsightDto`

**Tabla:** `RubroResumenDto`
* Rubro, total, participación %, comprobantes, artículos distintos, variación, ticket promedio, última compra

**Ficha de rubro:** `RubroDetalleDto`
* Evolución mensual, top artículos, top proveedores, últimos comprobantes

---

### Familias

**KPIs:** `FamiliasKpiDto`
* Total comprado, familias activas, familia principal y su participación
* Variación vs anterior, concentración Top 5
* Mayor crecimiento, mayor caída

**Gráficos:**
* Top familias, distribución del gasto
* Concentración Top 5 vs resto
* Familias en crecimiento / en caída

**Insights automáticos:** `FamiliasInsightDto`

**Tabla:** `FamiliaResumenDto`
* Familia, descripción, padre, nivel jerárquico, tiene hijos
* Total, participación %, artículos, proveedores, comprobantes
* Variación, ticket promedio, última compra

**Ficha de familia:** `FamiliaDetalleDto`
* Composición interna (subfamilias/artículos), artículos, proveedores
* Evolución mensual, últimos comprobantes

---

### Artículos

**KPIs:** `ArticulosKpiDto`
* Total comprado, artículos distintos, total ítems, costo promedio general
* Artículos con aumento / con baja de precio
* Mayor aumento (artículo + variación %) / Mayor baja

**Gráficos:**
* Top por impacto económico (HorizontalBarChart)
* Top por cantidad comprada (HorizontalBarChart)
* Top aumentos de precio
* Top bajas de precio

**Insights automáticos:** `ArticulosInsightDto`

**Tabla:** `ArticuloResumenDto`
* Artículo (código + descripción), cantidad, total, costo promedio
* Precio anterior / precio actual, variación %
* Última compra, cantidad de compras, proveedor principal y su participación %

**Ficha de artículo:** `ArticuloDetalleDto`
* Evolución de costo mensual (LineChart)
* Desglose por proveedor (HorizontalBarChart)
* Historial de comprobantes

---

### Actividad

**KPIs:** `ActividadKpisDto`
* Facturas cargadas, ítems cargados, usuarios activos
* Promedio ítems/factura, promedio facturas/usuario
* Día de mayor actividad, usuario más activo, usuario con mayor detalle

**Gráficos diarios:**
* Facturas por día (LineChart)
* Ítems por día (LineChart)
* Segmentación tipo de carga (con detalle vs contables)

**Gráficos por usuario:**
* Facturas por usuario (HorizontalBarChart)
* Ítems por usuario (HorizontalBarChart)

**Tabla de usuarios:** `ActividadUsuarioResumenDto`
* Usuario, facturas, ítems, ítems/factura, importe total
* Última actividad, días activos, % con detalle, % contables

**Ficha de usuario:** `ActividadUsuarioDetalleDto`
* Serie diaria de facturas e ítems
* Listado completo de comprobantes del usuario

---

## 🔎 Filtros globales

Persisten entre páginas via `FilterStateService`.

| Filtro | Tipo | Descripción |
|---|---|---|
| FechaDesde / FechaHasta | Fecha | Período de análisis |
| Proveedor | Texto | Búsqueda parcial en cuenta o razón social |
| ArticuloCodigo | Texto | Coincidencia exacta en código |
| ArticuloDescripcion | Texto | Búsqueda parcial en descripción |
| Rubro | Selector | Dropdown con valores de la vista |
| Familia | Selector | Dropdown con valores de la vista |
| Usuario | Selector | Dropdown con usuarios activos |
| Sucursal | Selector | Dropdown con sucursales |
| Deposito | Selector | Dropdown con depósitos |
| Estado | Selector | Estado del comprobante |
| TipoComprobante | Selector | TC: FCC, NCC, etc. |

**Accesos rápidos de fecha:** Hoy / Esta semana / Este mes / Mes anterior / Últimos 3 meses / Año actual

---

## 🧩 Componentes compartidos

| Componente | Descripción |
|---|---|
| `GlobalFiltersBar` | Barra de filtros + accesos rápidos + resumen de filtros activos |
| `KpiCard` | Tarjeta de indicador con ícono, título, valor y descripción |
| `LineChart` | Gráfico de línea SVG con área, grilla, puntos y ejes |
| `HorizontalBarChart` | Barras horizontales SVG con etiquetas, valores y links opcionales |
| `DataTable` | Grilla genérica con headers configurables y toolbar |
| `DetailCard` | Panel lateral de detalle con filas clave/valor |

---

## 🧩 Servicios

### `ComprasDashboardService` (clase parcial)

| Archivo | Responsabilidad |
|---|---|
| `ComprasDashboardService.cs` | Configuración, conexión, helpers SQL, logging de consultas |
| `ComprasDashboardService.Dashboard.cs` | KPIs del resumen general (`GetDashboardAsync`, `GetKpiSummaryAsync`) |
| `ComprasDashboardService.Comprobantes.cs` | Listado paginado, overview y detalle de comprobantes |
| `ComprasDashboardService.Entities.cs` | Proveedores, rubros, familias, artículos (resumen + detalle) |
| `ComprasDashboardService.Activity.cs` | Actividad por usuario y por día |
| `ComprasDashboardService.Shared.cs` | Métodos utilitarios de lectura y cálculo compartidos |

### `FilterStateService`

Singleton de Blazor que mantiene el estado de `DashboardFilters` entre navegaciones de página.

### `ServerStartupHostedService`

Tarea de inicio que abre el navegador automáticamente al levantar el servidor (configurable).

---

## 🗂️ Modelos de datos (DTOs)

### Compartidos

| DTO | Uso |
|---|---|
| `DashboardFilters` | Parámetros de filtro para todas las consultas |
| `ComprobantesFilter` | Extiende `DashboardFilters` con paginación |
| `FilterOptionsDto` | Listas para poblar los selectores de filtros |
| `MonthlyPointDto` | Punto de serie temporal (período + total) para LineChart |
| `CategoryTotalDto` | Ítem de ranking (categoría, código, total, participación %) |
| `StatusMetricDto` | Métrica por estado (estado, cantidad, total) |

### Por sección

| DTO | Sección |
|---|---|
| `DashboardSummaryDto` | Inicio |
| `ComprobantesOverviewDto`, `ComprobantesResultDto`, `ComprobanteDto`, `ComprobanteDetalleDto`, `ComprobanteItemDto`, `ComprobantesAlertDto` | Comprobantes |
| `ProveedoresPageDto`, `ProveedoresKpiDto`, `ProveedorResumenDto`, `ProveedorDetalleDto` | Proveedores |
| `RubrosPageDto`, `RubrosKpiDto`, `RubroResumenDto`, `RubroDetalleDto`, `RubrosInsightDto` | Rubros |
| `FamiliasPageDto`, `FamiliasKpiDto`, `FamiliaResumenDto`, `FamiliaDetalleDto`, `FamiliasInsightDto` | Familias |
| `ArticulosPageDto`, `ArticulosKpiDto`, `ArticuloResumenDto`, `ArticuloDetalleDto`, `ArticulosInsightDto` | Artículos |
| `ActividadPageDto`, `ActividadKpisDto`, `ActividadUsuarioResumenDto`, `ActividadDiaDto`, `ActividadUsuarioDetalleDto`, `ActividadInsightDto` | Actividad |

---

## 🧭 Flujos de navegación

```
Inicio → Proveedores → Ficha proveedor → ver artículos / comprobantes
Inicio → Rubros → Ficha rubro → ver artículos / comprobantes
Inicio → Familias → Ficha familia → ver subfamilias / artículos
Inicio → Artículos → Ficha artículo → ver evolución de costo / proveedores
Comprobantes → Detalle comprobante → ver líneas de ítems
Actividad → Ficha usuario → ver serie diaria / comprobantes del usuario
```

---

## ⚙️ Configuración (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "AlfaGestion": "Server=...;Database=...;..."
  },
  "DatosSql": {
    "CommandTimeoutSegundos": 30,
    "LogConsultasLentasDesdeMs": 1000
  },
  "ServidorWeb": {
    "Puerto": 5055,
    "EscucharEnRed": true,
    "AbrirNavegadorAlIniciar": true,
    "Protocolo": "http",
    "UrlBasePublica": "",
    "NombreAplicacion": "Dashboard de Compras - Alfa Gestión"
  }
}
```

---

## 🚀 Deploy

* Publicar con `scripts\publicar_release.bat` → genera `publish\DashboardComprasLAN`
* Copiar carpeta al servidor
* Editar `appsettings.Production.json` con cadena de conexión, puerto y opciones
* Ejecutar `abrir_firewall.bat` como administrador (una sola vez)
* Iniciar con `iniciar_dashboard.bat`
* Acceso en red: `http://NOMBRE-SERVIDOR:5055` o `http://IP:5055`

---

## 🧠 Objetivo final

Convertir Alfa Gestión en una plataforma con análisis tipo BI para toma de decisiones en compras.
