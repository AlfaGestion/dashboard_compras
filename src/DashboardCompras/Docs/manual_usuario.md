# Manual de Usuario — Dashboard de Compras
### Alfa Gestión · v1.0

---

## Índice

1. [¿Qué es el Dashboard de Compras?](#1-qué-es-el-dashboard-de-compras)
2. [Acceso y navegación general](#2-acceso-y-navegación-general)
3. [Barra de filtros globales](#3-barra-de-filtros-globales)
4. [Inicio — Resumen ejecutivo](#4-inicio--resumen-ejecutivo)
5. [Proveedores](#5-proveedores)
6. [Comprobantes](#6-comprobantes)
7. [Rubros](#7-rubros)
8. [Familias](#8-familias)
9. [Artículos](#9-artículos)
10. [Actividad](#10-actividad)
11. [InformesIA](#11-informesia)
12. [Conceptos y métricas clave](#12-conceptos-y-métricas-clave)
13. [Preguntas frecuentes](#13-preguntas-frecuentes)
14. [Información técnica de vistas](#14-información-técnica-de-vistas)

---

## 1. ¿Qué es el Dashboard de Compras?

El **Dashboard de Compras** es una herramienta de análisis de datos integrada con el sistema **Alfa Gestión**. Transforma la información de compras registrada en el ERP en indicadores visuales, gráficos y tablas que permiten tomar decisiones informadas sobre:

- Concentración del gasto por proveedor o categoría.
- Evolución del gasto en el tiempo.
- Comportamiento de precios de artículos.
- Productividad operativa de los usuarios que cargan comprobantes.
- Detección de comprobantes con anomalías (valor cero, sin detalle, IVA cero, etc.).

La aplicación **no modifica datos** del sistema. Solo los lee y los presenta de forma analítica.

---

## 2. Acceso y navegación general

### 2.1 Abrir la aplicación

La aplicación se ejecuta como un servidor web local. Accedés desde cualquier navegador ingresando la dirección que te indique el área de sistemas, por ejemplo:

```
http://NOMBRE-SERVIDOR:5055
```

Si estás en la misma PC donde corre el servidor, también podés usar:

```
http://localhost:5055
```

### 2.2 Menú principal

El menú lateral izquierdo contiene las secciones principales:

| Sección | ¿Qué muestra? |
|---|---|
| **Inicio** | Resumen ejecutivo con los KPI más importantes |
| **Proveedores** | Análisis del gasto por proveedor |
| **Comprobantes** | Listado y detalle de facturas y notas de crédito |
| **Rubros** | Gasto agrupado por categorías de producto |
| **Familias** | Gasto por familias jerárquicas de productos |
| **Artículos** | Análisis a nivel de artículo individual y evolución de precios |
| **Actividad** | Métricas de carga operativa por usuario |
| **InformesIA** | Consultas en lenguaje natural sobre las vistas autorizadas del dashboard |
| **Ayuda** | Manual de uso con buscador e índice rápido |

Hacé clic en cualquier sección para navegar. Los filtros que tengas activos **se mantienen** al cambiar de sección.

### 2.3 Tarjetas KPI

En la parte superior de cada sección encontrás tarjetas con indicadores clave. Cada tarjeta muestra:

- **Ícono** representativo del indicador.
- **Título** descriptivo.
- **Valor principal** (monto, cantidad, porcentaje, etc.).
- **Descripción adicional** cuando el indicador requiere contexto.

---

## 3. Barra de filtros globales

Todas las secciones comparten la misma barra de filtros ubicada en la parte superior de la página. Los filtros aplicados afectan **todos los datos visibles** en la sección actual.

### 3.1 Rango de fechas

El filtro más usado. Podés definir el período de análisis de dos maneras:

- **Manualmente:** Ingresá las fechas "Desde" y "Hasta" en los campos de texto.
- **Accesos rápidos:** Botones preconfigurados para los períodos más comunes:

| Botón | Período que aplica |
|---|---|
| Hoy | Solo el día de hoy |
| Esta semana | Desde el lunes de la semana actual hasta hoy |
| Este mes | Desde el primer día del mes hasta hoy |
| Mes anterior | Mes calendario anterior completo |
| Últimos 3 meses | Los tres meses anteriores completos |
| Año actual | Desde el 1 de enero hasta hoy |

### 3.2 Filtros avanzados

Hacé clic en **"Filtros avanzados"** para expandir el panel con las siguientes opciones:

| Filtro | Descripción |
|---|---|
| **Proveedor** | Busca por cuenta o razón social (búsqueda parcial) |
| **Código de artículo** | Coincidencia exacta con el código interno |
| **Descripción de artículo** | Búsqueda parcial en la descripción |
| **Rubro** | Selector desplegable con los rubros disponibles |
| **Familia** | Selector desplegable con las familias disponibles |
| **Usuario** | Filtra por el usuario que cargó los comprobantes |
| **Sucursal** | Filtra por sucursal o punto de venta |
| **Depósito** | Filtra por depósito o almacén |
| **Estado** | Estado del comprobante en el sistema |
| **Tipo de comprobante** | FCC, NCC, NDCC, etc. |

### 3.3 Barra de filtros activos

Debajo de la barra de filtros aparece un resumen de los filtros activos en ese momento. Esto te permite saber qué condiciones está aplicando el sistema sin tener que abrir el panel cada vez.

### 3.4 Aplicar y limpiar filtros

- Para aplicar filtros, hacé clic en **"Aplicar"** o presioná Enter.
- Para quitar todos los filtros, hacé clic en **"Limpiar"**.

---

## 4. Inicio — Resumen ejecutivo

La pantalla de inicio ofrece una visión rápida del estado de las compras en el período seleccionado.

### 4.1 KPI principales

| Indicador | Descripción |
|---|---|
| **Total comprado** | Suma de todos los importes con signo aplicado (facturas menos notas de crédito) |
| **Neto total** | Monto sin IVA |
| **IVA total** | Monto de IVA acumulado |
| **Comprobantes** | Cantidad de comprobantes en el período |
| **Artículos distintos** | Cantidad de artículos únicos comprados |
| **Proveedores activos** | Cantidad de proveedores con al menos una compra |
| **Ticket promedio** | Promedio de importe por comprobante |

### 4.2 Evolución mensual

Gráfico de línea que muestra el gasto total mes a mes en los últimos 12 meses. Permite identificar tendencias, estacionalidad y meses atípicos.

### 4.3 Top 7 Proveedores

Gráfico de barras horizontales con los 7 proveedores de mayor importe de compra. Hacé clic en un proveedor para ir directamente a su análisis detallado.

### 4.4 Top 7 Rubros

Gráfico de barras horizontales con los 7 rubros de mayor participación en el gasto total.

### 4.5 Top 10 Artículos

Tabla con los 10 artículos de mayor impacto económico. Hacé clic en cualquier fila para ver el historial completo del artículo.

---

## 5. Proveedores

### 5.1 KPI de proveedores

| Indicador | Descripción |
|---|---|
| **Total comprado** | Importe total del período filtrado |
| **Proveedores activos** | Cantidad de proveedores distintos |
| **Proveedor principal** | El de mayor importe |
| **Concentración Top 5** | Qué porcentaje del gasto total representan los 5 primeros |
| **Variación vs período anterior** | Variación porcentual respecto al período equivalente anterior |
| **Mayor crecimiento** | Proveedor con mayor incremento relativo |
| **Mayor caída** | Proveedor con mayor disminución relativa |

### 5.2 Gráficos

- **Ranking de proveedores:** Top 10 por importe total.
- **Concentración:** Muestra la participación del Top 5 vs. el resto, útil para evaluar dependencia de proveedores.

### 5.3 Tabla de proveedores

La tabla detallada muestra para cada proveedor:

| Columna | Descripción |
|---|---|
| Proveedor | Cuenta y razón social |
| Total | Importe comprado en el período |
| Participación | % del gasto total |
| Comprobantes | Cantidad de facturas |
| Ticket promedio | Promedio por comprobante |
| Última compra | Fecha del último comprobante |
| Variación | Diferencia % vs período anterior |
| Ficha | Botón para ver el detalle del proveedor |

### 5.4 Ficha de proveedor

Al hacer clic en **"Ficha"** de cualquier proveedor se abre un panel lateral con:

- **Artículos principales:** Los artículos más comprados a ese proveedor.
- **Últimas facturas:** Los comprobantes más recientes.
- **Evolución mensual:** Gráfico de línea con el gasto mensual en ese proveedor.

---

## 6. Comprobantes

### 6.1 KPI de comprobantes

| Indicador | Descripción |
|---|---|
| **Importe total** | Suma total del período |
| **Cantidad** | Número de comprobantes |
| **Proveedores** | Proveedores distintos involucrados |
| **Ticket promedio** | Promedio por comprobante |
| **Sin detalle** | Comprobantes sin líneas de detalle cargadas |
| **Valor cero** | Comprobantes con importe $0 |
| **Mayor comprobante** | El comprobante de mayor importe |

### 6.2 Distribución por tipo

Muestra qué tipos de comprobante predominan (FCC, NCC, NDCC, etc.) y su participación en el total.

### 6.3 Evolución semanal

Gráfico de línea que muestra el gasto semana a semana dentro del período filtrado.

### 6.4 Alertas

El sistema identifica automáticamente situaciones que pueden requerir revisión:

- Comprobantes con importe cero.
- Comprobantes sin detalle de artículos.
- Comprobantes con IVA cero en importes significativos.

### 6.5 Filtros rápidos

Botones sobre la tabla que aplican filtros de un clic:

| Botón | Qué muestra |
|---|---|
| Críticos | Comprobantes con alertas activas |
| Con detalle | Solo los que tienen líneas cargadas |
| Asientos contables | Comprobantes cargados como asiento |
| Valor cero | Importes igual a $0 |
| IVA cero | Sin IVA declarado |
| Sin detalle | Los que no tienen líneas de artículos |

### 6.6 Tabla de comprobantes

La tabla está paginada de a 20 registros. Para cada comprobante se muestra:

| Columna | Descripción |
|---|---|
| Fecha | Fecha del comprobante |
| TC | Tipo de comprobante |
| Comprobante | Identificador visible del comprobante (`IDCOMPROBANTE`) |
| Proveedor | Cuenta y razón social |
| Cuenta | Cuenta contable asociada |
| Neto | Importe sin IVA |
| IVA | Importe de IVA |
| Total | Importe total |
| Estado | Estado en el sistema |
| Detalle | Tiene detalle sí/no |
| Ítems | Cantidad de líneas de detalle |
| Tipo | Con detalle o asiento contable |
| Alertas | Indicadores de situaciones anómalas |
| Depósito | Depósito destino |
| Usuario | Usuario que lo cargó |

### 6.7 Detalle de comprobante

Hacé clic en cualquier fila para ver el modal de detalle con todas las líneas del comprobante: artículo, descripción, cantidad, precio unitario y total.

---

## 7. Rubros

Los **rubros** son la categorización principal de los artículos en Alfa Gestión.

### 7.1 KPI de rubros

| Indicador | Descripción |
|---|---|
| **Total comprado** | Importe del período |
| **Rubros activos** | Categorías con al menos una compra |
| **Rubro principal** | El de mayor gasto |
| **Participación del líder** | Qué % del total representa el rubro principal |
| **Concentración Top 3** | Los tres primeros rubros como % del total |
| **Mayor crecimiento** | Rubro con mayor aumento vs período anterior |
| **Mayor caída** | Rubro con mayor disminución |

### 7.2 Gráficos

- **Top rubros:** Ranking de los rubros con mayor gasto.
- **Distribución:** Gráfico de participación porcentual de cada rubro.
- **Concentración:** Top 3 vs. resto.
- **En crecimiento:** Rubros con variación positiva vs período anterior.
- **En caída:** Rubros con variación negativa.

### 7.3 Tabla de rubros

| Columna | Descripción |
|---|---|
| Rubro | Código y descripción |
| Total | Importe comprado |
| Participación | % del total general |
| Comprobantes | Facturas que incluyen este rubro |
| Artículos | Artículos distintos del rubro |
| Variación | Cambio % vs período anterior |
| Ticket promedio | Promedio por comprobante |
| Última compra | Fecha más reciente |

### 7.4 Ficha de rubro

Al hacer clic en un rubro se abre un panel con:

- **Composición interna:** Artículos dentro del rubro.
- **Evolución mensual:** Gasto mensual en el rubro.
- **Top artículos:** Los artículos más significativos.
- **Top proveedores:** Los proveedores más relevantes para ese rubro.
- **Últimas facturas:** Comprobantes más recientes que incluyen el rubro.

---

## 8. Familias

Las **familias** son una categorización jerárquica de artículos (pueden tener subfamilias).

La estructura y funcionalidad es idéntica a la de [Rubros](#7-rubros), con las siguientes diferencias:

- Se muestra la **jerarquía**: familia padre / subfamilia.
- La tabla incluye columnas de **nivel** (0 = raíz, 1 = subfamilia, etc.) y **tiene hijos** (sí/no).
- La ficha incluye un desglose de la **composición interna** por subfamilias o artículos.

---

## 9. Artículos

### 9.1 KPI de artículos

| Indicador | Descripción |
|---|---|
| **Total comprado** | Importe del período |
| **Artículos distintos** | Cantidad de artículos únicos |
| **Total ítems** | Suma de unidades compradas |
| **Costo promedio general** | Promedio ponderado de costo unitario |
| **Con aumento de precio** | Artículos cuyo precio subió vs período anterior |
| **Con baja de precio** | Artículos cuyo precio bajó |
| **Mayor aumento** | Artículo con mayor incremento porcentual de precio |
| **Mayor caída** | Artículo con mayor disminución porcentual |

### 9.2 Gráficos

- **Top por impacto económico:** Los artículos que más pesan en el gasto total.
- **Top por cantidad:** Los artículos que se compran en mayor volumen.
- **Mayores aumentos de precio:** Los que más subieron vs el período anterior.
- **Mayores bajas de precio:** Los que más bajaron.

### 9.3 Tabla de artículos

| Columna | Descripción |
|---|---|
| Artículo | Código y descripción |
| Cantidad | Unidades compradas |
| Total | Importe total |
| Costo promedio | Precio unitario promedio en el período |
| Precio anterior | Costo promedio del período comparativo |
| Precio actual | Costo promedio del período actual |
| Variación | Cambio % de precio |
| Última compra | Fecha más reciente |
| Compras | Cantidad de comprobantes donde aparece |
| Proveedor principal | El proveedor que más lo vende |
| % del proveedor | Participación del proveedor principal en ese artículo |

### 9.4 Ficha de artículo

Al hacer clic en un artículo se abre un panel con:

- **Evolución de costo mensual:** Gráfico de línea con la variación del precio a lo largo del tiempo.
- **Desglose por proveedor:** Qué proporción compra a cada proveedor.
- **Historial de compras:** Comprobantes donde apareció el artículo con fecha, proveedor, precio y cantidad.

---

## 10. Actividad

La sección **Actividad** analiza la carga operativa: quién cargó comprobantes, cuántos, y en qué fechas.

### 10.1 KPI de actividad

| Indicador | Descripción |
|---|---|
| **Facturas cargadas** | Total de comprobantes ingresados en el período |
| **Ítems cargados** | Total de líneas de detalle ingresadas |
| **Usuarios activos** | Usuarios distintos con actividad |
| **Ítems por factura** | Promedio de líneas por comprobante |
| **Facturas por usuario** | Promedio de comprobantes por usuario |
| **Día de mayor actividad** | La fecha con más comprobantes cargados |
| **Usuario más activo** | El que más comprobantes cargó |
| **Mayor detalle** | El usuario con más líneas de artículo |

### 10.2 Gráficos diarios

- **Facturas por día:** Evolución diaria de comprobantes cargados.
- **Ítems por día:** Evolución diaria de líneas ingresadas.
- **Distribución por tipo:** Proporción entre comprobantes con detalle de artículos y asientos contables.

### 10.3 Gráficos por usuario

- **Facturas por usuario:** Ranking de usuarios por cantidad de comprobantes.
- **Ítems por usuario:** Ranking de usuarios por líneas de detalle ingresadas.

### 10.4 Tabla de usuarios

| Columna | Descripción |
|---|---|
| Usuario | Nombre de usuario |
| Facturas | Comprobantes cargados |
| Ítems | Líneas de detalle |
| Ítems/Factura | Promedio de detalle por comprobante |
| Importe | Total de importe manejado |
| Última actividad | Último comprobante cargado |
| Días activos | Días distintos con actividad |
| Con detalle | % de comprobantes con líneas de artículo |
| Contables | % de comprobantes solo con asiento |

### 10.5 Ficha de usuario

Al hacer clic en un usuario se abre un panel con:

- **Serie diaria de facturas:** Actividad día a día.
- **Serie diaria de ítems:** Cantidad de líneas por día.
- **Listado de comprobantes:** Todos los comprobantes cargados por ese usuario en el período.

---

## 11. InformesIA

La sección **InformesIA** permite generar informes puntuales en lenguaje natural sobre la misma información visible en el dashboard, sin modificar datos.

### 11.1 Qué hace

- Interpreta consultas como "proveedores con mayor crecimiento", "rubros con más participación" o "listado de compras del mes".
- Trabaja en **solo lectura**.
- Usa únicamente las vistas autorizadas del tablero.
- Devuelve resultados en una pestaña nueva para no pisar informes anteriores.

### 11.2 Cómo usarla

1. Escribí la consulta en el cuadro principal.
2. Si querés, ajustá el rango desde los filtros globales.
3. Marcá o desmarcá **Incluir gráfico** según el tipo de salida que necesitás.
4. Hacé clic en **Abrir informe en nueva pestaña**.

### 11.3 Sugerencias y consultas soportadas

La pantalla incluye sugerencias rápidas. Además, reconoce consultas orientadas a:

- Rankings: top proveedores, artículos, rubros o comprobantes.
- Comparaciones: crecimiento, caída, variación contra período anterior.
- Concentración: participación del gasto por proveedor, rubro o familia.
- Evolución: compras por día o por mes.
- Listados: comprobantes del período con columnas pedidas por el usuario.

### 11.4 Reglas importantes

- Si la consulta incluye un período explícito, por ejemplo **"marzo 2026"** o **"últimos 7 días"**, ese período tiene prioridad sobre la barra de filtros.
- Si el informe no puede resolverse con las vistas autorizadas, el sistema lo rechaza con un mensaje claro.
- La validación bloquea comandos peligrosos y cualquier intento de escritura o acceso fuera del alcance permitido.

### 11.5 Historial, exportación y dictado

- **Historial:** guarda las últimas consultas para reejecutarlas o reutilizarlas.
- **PDF:** cada resultado puede exportarse a PDF desde la vista del informe.
- **Dictado:** el botón de micrófono permite cargar una consulta por voz si el navegador lo soporta.

### 11.6 Fuentes autorizadas

InformesIA usa solo estas vistas:

- `vw_compras_cabecera_dashboard`
- `vw_compras_detalle_dashboard`
- `vw_estadisticas_ingresos_diarias`
- `vw_familias_jerarquia`

El detalle técnico de cada una está documentado al final de este manual.

---

## 12. Conceptos y métricas clave

### Sistema de signo

Los comprobantes pueden ser:
- **Facturas (FCC, FPC, LIQC, NDC):** suman al gasto → signo **+1**
- **Notas de crédito (NCC, NCPC):** restan del gasto → signo **-1**

Todos los importes del dashboard ya tienen el signo aplicado. Si una nota de crédito aparece en el análisis, ya está descontando del total.

### Variación vs período anterior

Cuando el sistema muestra **"Variación"**, compara el período filtrado con el período inmediatamente anterior de la misma duración.

Ejemplo: si filtrás del 1 al 31 de marzo, la variación se calcula contra el 1 al 28 de febrero.

**Interpretación:**
- Valor positivo (verde) → el gasto/precio aumentó.
- Valor negativo (rojo) → el gasto/precio disminuyó.

### Participación %

Es el peso relativo de un ítem (proveedor, rubro, artículo) dentro del total del período:

```
Participación = (Total del ítem / Total general) × 100
```

### Concentración

Indica qué porcentaje del gasto total está en los N principales items. Una concentración alta en pocos proveedores puede representar un riesgo de dependencia.

### Ticket promedio

Importe medio por comprobante:

```
Ticket promedio = Total comprado / Cantidad de comprobantes
```

### Costo promedio de artículo

Precio unitario promedio en el período:

```
Costo promedio = Total comprado del artículo / Cantidad de unidades
```

---

## 13. Preguntas frecuentes

**¿Por qué los datos no coinciden exactamente con los del sistema Alfa Gestión?**

El dashboard aplica el sistema de signo de compras: las notas de crédito se restan de las facturas. En Alfa Gestión podés ver los importes brutos de cada comprobante por separado; aquí se muestran los netos analíticos.

**¿Los filtros se pierden al cambiar de sección?**

No. Los filtros se mantienen mientras no los limpies vos manualmente. Esto permite, por ejemplo, filtrar por un proveedor en la sección **Proveedores** y luego ver sus comprobantes en **Comprobantes** sin reconfigurar el filtro.

**¿Puedo exportar la información?**

Sí. La aplicación permite exportar a **PDF** la pantalla actual y también los resultados de **InformesIA**. La exportación conserva el contenido visible, incluyendo tablas, resumen y gráficos cuando corresponden.

**¿Con qué frecuencia se actualizan los datos?**

Los datos se leen en tiempo real de la base de datos de Alfa Gestión cada vez que navegás a una sección o aplicás un filtro. No hay caché permanente.

**¿Qué significa un comprobante "sin detalle"?**

Es un comprobante que fue cargado solo con el importe total (como asiento contable), sin especificar los artículos comprados. Esto puede afectar los análisis de artículos y rubros, ya que esas compras no quedan clasificadas.

**¿El dashboard modifica algo en Alfa Gestión?**

No. El dashboard es de **solo lectura**. Ninguna acción dentro de la aplicación modifica datos del sistema.

**¿Puedo ver el dashboard desde otra computadora de la red?**

Sí, siempre que el servidor esté configurado para escuchar en la red y el firewall esté habilitado. Consultá con el área de sistemas la dirección IP o nombre del servidor y el puerto configurado.

---

## 14. Información técnica de vistas

Esta sección resume las vistas usadas por el dashboard y por InformesIA. Está pensada como referencia técnica para soporte, parametrización y evolución funcional.

### 14.1 `vw_compras_cabecera_dashboard`

**Utilidad**

- Vista principal de comprobantes de compras.
- Alimenta KPI generales, comprobantes, proveedores, actividad por usuario y listados tipo libro IVA compras.
- Se usa también como base de filtros globales por proveedor, usuario, sucursal, depósito, estado y tipo de comprobante.

**Campos utilizados con más frecuencia**

| Campo | Utilidad en la aplicación |
|---|---|
| `TC` | Tipo de comprobante; identifica FCC, NCC, LIQC, etc. |
| `IDCOMPROBANTE` | Identificador interno del comprobante; se usa para joins y detalle. |
| `IDCOMPROBANTE` | Identificador completo del comprobante, usado como número visible. En la práctica reúne sucursal, letra y número. |
| `NUMERO` | Componente interno de numeración simple; no se muestra aislado como identificador principal. |
| `FECHA` | Fecha de emisión o registración; base de filtros y series temporales. |
| `CUENTA` | Código o cuenta del proveedor. |
| `RAZON_SOCIAL` | Nombre del proveedor. |
| `NetoDashboard` | Importe neto sin IVA con signo analítico aplicado. |
| `IvaDashboard` | Importe de IVA del comprobante. |
| `ImporteDashboard` | Total del comprobante; base de totales, rankings y tickets promedio. |
| `EstadoComprobante` | Estado operativo del comprobante. |
| `USUARIO` | Usuario que cargó el comprobante; base de la sección Actividad. |
| `SUCURSAL` | Sucursal o punto de emisión. |
| `IdDeposito` | Depósito asociado a la operación. |

### 14.2 `vw_compras_detalle_dashboard`

**Utilidad**

- Vista de detalle por ítem o línea de comprobante.
- Alimenta análisis de artículos, rubros, familias, composición interna y filtros avanzados por artículo.
- Se usa para saber si un comprobante tiene detalle y para vincular cabecera con líneas.

**Campos utilizados con más frecuencia**

| Campo | Utilidad en la aplicación |
|---|---|
| `TC` | Tipo de comprobante; clave de relación con la cabecera. |
| `IDCOMPROBANTE` | Identificador del comprobante en el detalle. |
| `CUENTA` | Cuenta del proveedor asociada al comprobante. |
| `FECHA` | Fecha de la línea o del comprobante, usada en análisis temporales. |
| `IDARTICULO` | Código interno del artículo. |
| `DESCRIPCION_ARTICULO` | Descripción principal del artículo. |
| `DESCRIPCION_ITEM` | Texto alternativo de la línea cuando no hay descripción estándar. |
| `RUBRO` | Rubro del artículo para análisis por categoría. |
| `FAMILIA` | Familia del artículo; permite navegación jerárquica. |
| `CantidadDashboard` | Cantidad analítica comprada. |
| `COSTO` | Costo unitario o precio de la línea. |
| `TotalDashboard` | Importe total de la línea; base de rankings y participaciones. |
| `USUARIO` | Usuario que cargó la operación cuando se analiza actividad por detalle. |
| `SUCURSAL` | Sucursal de la operación. |
| `IdDeposito` | Depósito vinculado a la línea. |

### 14.3 `vw_estadisticas_ingresos_diarias`

**Utilidad**

- Vista autorizada para InformesIA orientada a series y resúmenes diarios.
- Está prevista para consultas de evolución temporal, actividad o ingresos consolidados por fecha.
- En la versión actual del dashboard su participación es complementaria y se reserva principalmente para ampliaciones de InformesIA.

**Campos esperados de referencia**

Los nombres exactos pueden variar según el entorno, pero su uso esperado es sobre columnas de:

- fecha diaria,
- cantidad de registros o comprobantes,
- importe diario,
- usuario o agrupador operativo,
- tipo de movimiento o clasificación.

Si esta vista se amplía o cambia en base de datos, conviene actualizar este manual junto con las plantillas de InformesIA.

### 14.4 `vw_familias_jerarquia`

**Utilidad**

- Vista de apoyo para resolver jerarquías de familias.
- Permite mostrar árbol padre-hijo, nivel jerárquico y análisis agregados por ramas.
- Se usa en Familias y en informes IA que requieren composición o crecimiento por familia.

**Campos utilizados con más frecuencia**

| Campo | Utilidad en la aplicación |
|---|---|
| `IdFamilia` | Identificador o código de la familia. |
| `Descripcion` | Nombre visible de la familia. |
| `PadreIdFamilia` | Familia padre; permite reconstruir la jerarquía. |
| `NivelJerarquico` | Profundidad dentro del árbol de familias. |
| `TieneHijos` | Indicador lógico para saber si la familia tiene subfamilias. |

### 14.5 Relación entre vistas

- `vw_compras_cabecera_dashboard` representa el comprobante completo.
- `vw_compras_detalle_dashboard` representa sus líneas o ítems.
- `vw_familias_jerarquia` aporta la estructura para interpretar familias y subfamilias.
- `vw_estadisticas_ingresos_diarias` sirve como apoyo para resúmenes temporales diarios en escenarios de IA y evolución.

En términos funcionales, la aplicación combina estas vistas para responder preguntas de negocio sin consultar tablas operativas directamente.

---

*Manual de Usuario — Dashboard de Compras — Alfa Gestión*
*Última actualización: abril 2026*
