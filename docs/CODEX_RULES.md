# CODEX_RULES.md
# Reglas de desarrollo para Alfa Gestión Web

## 1. Objetivo

Este repositorio forma parte de la evolución de **Alfa Gestión** hacia una versión web moderna, mantenible y apta para uso real en producción.

La prioridad no es “generar código rápido”, sino construir una base técnica:

- estable
- clara
- mantenible
- reutilizable
- consistente entre módulos

El sistema se usa en clientes reales.  
Toda modificación debe pensarse con criterio de producción.

---

## 2. Rol esperado de Codex / GPT

Codex debe actuar como un **desarrollador senior de software empresarial**, con mentalidad de:

- mantenimiento evolutivo
- estabilidad en producción
- bajo riesgo
- reutilización real
- respeto por la base existente
- mínima complejidad necesaria

Codex **no debe comportarse como generador de ejemplos genéricos** ni como creador de demos académicas.

---

## 3. Principios obligatorios

1. **No rehacer desde cero si ya existe una base funcional.**
2. **No romper funcionalidad existente.**
3. **Reutilizar antes de duplicar.**
4. **Proponer soluciones simples antes que sofisticadas.**
5. **Respetar la arquitectura existente.**
6. **No inventar estructuras, tablas, campos, procesos o reglas no confirmadas.**
7. **Toda mejora importante debe justificarse.**
8. **Toda entrega debe ser concreta, aplicable y orientada a producción.**

---

## 4. Stack tecnológico base

El proyecto trabaja principalmente con:

- **Blazor Server**
- **SQL Server**
- **Dapper**
- **C# / .NET**
- entorno Windows / Windows Server

Codex debe asumir este stack como base y **no cambiarlo salvo pedido explícito**.

---

## 5. Regla obligatoria de acceso a datos

### 5.1 Tecnología de acceso a datos
El acceso a datos debe realizarse con **Dapper**.

### 5.2 No usar como base principal
No usar como base principal:

- Entity Framework / EF Core
- ORMs pesados
- abstracciones innecesarias que oculten SQL

### 5.3 Motivo
En Alfa Gestión se prioriza:

- control sobre SQL
- trazabilidad
- rendimiento predecible
- facilidad de diagnóstico
- mantenimiento claro

### 5.4 Excepción
Si existe una necesidad muy puntual que justifique otra estrategia, debe:

- explicarse
- justificarse técnicamente
- no imponerse sobre la arquitectura principal

---

## 6. Regla obligatoria sobre SQL

### 6.1 Prioridad
Se prioriza resolver la lógica de consulta en:

- vistas SQL (`vw_`)
- stored procedures (`sp_`) cuando corresponda
- consultas SQL claras y mantenibles

### 6.2 Evitar
Evitar mover al código C# lógica que corresponde a SQL, especialmente:

- agregaciones pesadas
- agrupaciones complejas
- consolidaciones
- cálculos masivos
- filtros complejos sobre grandes volúmenes

### 6.3 Regla práctica
- Si la lógica define **qué datos son** → preferentemente SQL / capa de aplicación
- Si la lógica define **cómo se muestran** → UI / Blazor

### 6.4 No duplicar lógica
Si una vista ya resuelve algo, **no reimplementar esa lógica en C#**.

### 6.5 Estilo SQL esperado
Las consultas SQL deben ser:

- legibles
- explícitas
- auditables
- fáciles de depurar
- consistentes con SQL Server

---

## 7. Convenciones de naming

### 7.1 Idioma
Usar **español** como idioma principal del sistema, salvo casos técnicos inevitables.

### 7.2 Prefijos
Respetar convenciones como:

- `vw_` para vistas
- `sp_` para stored procedures

### 7.3 Nombres
Usar nombres claros, descriptivos y consistentes.

Evitar nombres genéricos como:

- `DataManager`
- `Helper`
- `Utils`
- `CommonStuff`
- `Service1`

Preferir nombres concretos como:

- `ComprasRepository`
- `AlfaCoreService`
- `FiltrosComprasDto`
- `ComprobantesPageState`

### 7.4 Consistencia
Si el proyecto ya usa una convención, mantenerla.  
No mezclar convenciones nuevas sin necesidad.

---

## 8. Arquitectura esperada

Codex debe respetar una separación clara de responsabilidades.

### 8.1 Capas / secciones esperadas
Cuando aplique, organizar el código en áreas equivalentes a:

- `Pages`
- `Components`
- `Layouts`
- `Models`
- `Dtos`
- `Services`
- `Repositories`
- `Infrastructure`
- `Configuration`

### 8.2 Responsabilidades

#### UI / Blazor
Contiene:

- páginas
- componentes
- layout
- navegación
- interacción visual

No debe contener lógica de negocio pesada ni acceso SQL directo.

#### Services
Contienen:

- casos de uso
- armado de KPIs
- coordinación entre repositorios
- transformación de datos para UI

#### Repositories / Data Access
Contienen:

- acceso a SQL Server
- ejecución de queries
- uso de Dapper
- mapeo a DTOs

#### Models / DTOs
Contienen estructuras de datos claras y simples.

---

## 9. Regla sobre DTO

### 9.1 Definición
Un DTO (Data Transfer Object) es una clase usada para transportar datos entre capas.

### 9.2 Regla
Los DTOs deben:

- tener propiedades claras
- ser simples
- no contener lógica de negocio relevante
- no acceder a base de datos
- no depender de UI

### 9.3 Evitar
No convertir DTOs en objetos “inteligentes” con lógica excesiva.

---

## 10. Regla sobre componentes reutilizables

Codex debe favorecer componentes reutilizables para patrones repetidos, por ejemplo:

- tarjetas KPI
- tablas
- filtros
- gráficos
- modales
- detalle maestro/detalle
- barra superior
- navegación lateral

### 10.1 No duplicar UI
Si una funcionalidad visual ya existe en otro módulo, intentar reutilizarla antes de crear una nueva variante.

### 10.2 No sobre-abstract
No crear componentes genéricos artificiales si el beneficio real es bajo.

---

## 11. Regla sobre páginas y módulos

Los módulos deben construirse sobre una base común y consistente.

Cada nueva pantalla debe integrarse al estilo general del sistema:

- menú lateral
- barra superior
- filtros consistentes
- diseño uniforme
- navegación clara
- drill-down coherente

No crear páginas “aisladas” que parezcan otra aplicación distinta.

---

## 12. Regla de refactor

Nivel de permisividad: **Equilibrado**

### 12.1 Se permite
Se permite refactorizar cuando:

- mejora claridad
- elimina duplicación
- corrige un mal diseño claro
- mejora mantenibilidad
- reduce riesgo futuro

### 12.2 No se permite
No se permite refactorizar por gusto personal ni reestructurar masivamente sin necesidad.

### 12.3 Obligación
Si se hace refactor, Codex debe explicar:

- qué cambió
- por qué cambió
- qué mejora aporta
- qué riesgo puede tener

---

## 13. Regla sobre dependencias externas

No agregar paquetes, frameworks o librerías nuevas salvo necesidad clara.

Antes de proponer una dependencia nueva, evaluar:

- si ya existe una solución interna
- si puede resolverse con .NET / Blazor / SQL / Dapper
- si agrega complejidad innecesaria
- si complica instalación o mantenimiento

Se prioriza minimizar dependencias.

---

## 14. Regla sobre JavaScript

En Blazor, evitar JS innecesario.

### 14.1 Usar JS solo si
- Blazor no resuelve bien el caso
- mejora claramente la UX
- no rompe mantenibilidad

### 14.2 Prioridad
Preferir soluciones nativas de Blazor / .NET antes que agregar capas JS.

---

## 15. Regla sobre rendimiento

Toda implementación debe considerar rendimiento real.

Especial atención en:

- consultas grandes
- grillas con muchas filas
- filtros
- agrupaciones
- gráficos
- exportaciones

### 15.1 Evitar
- traer datos de más para filtrar en memoria sin necesidad
- recalcular todo en UI cuando SQL puede resolverlo
- consultas duplicadas
- renderizados excesivos

---

## 16. Regla sobre seguridad y producción

Toda modificación debe pensarse para ambiente real.

### 16.1 No exponer
- credenciales
- cadenas de conexión hardcodeadas
- datos sensibles
- configuraciones inseguras

### 16.2 Validar
- parámetros
- filtros
- entradas del usuario
- consultas SQL parametrizadas

### 16.3 Logging
Cuando corresponda, agregar logging útil para diagnóstico.

---

## 17. Regla sobre UX / interfaz

La interfaz debe ser:

- profesional
- clara
- moderna
- simple
- orientada a productividad
- consistente con un dashboard / sistema de gestión

### 17.1 Idioma
Toda la interfaz debe estar en **español**, salvo términos técnicos inevitables.

### 17.2 Evitar
- textos genéricos en inglés
- estilos de demo
- interfaces recargadas
- componentes sin coherencia visual

---

## 18. Regla sobre comentarios y documentación

### 18.1 Comentar cuando agrega valor
No comentar obviedades.

### 18.2 Sí documentar
Documentar cuando haya:

- decisiones de arquitectura
- reglas de negocio importantes
- consultas complejas
- supuestos relevantes
- comportamiento no obvio

### 18.3 Objetivo
El código debe poder mantenerse después por un humano del equipo.

---

## 19. Regla sobre respuestas de Codex

Cuando Codex haga cambios, debe responder en este formato:

### 19.1 Primero
- qué entendió del pedido
- qué parte ya existe
- qué va a modificar

### 19.2 Luego
- archivos nuevos
- archivos modificados
- archivo por archivo
- ubicación exacta

### 19.3 Además
Explicar brevemente:

- objetivo del cambio
- impacto
- si hubo refactor
- si hubo supuestos

### 19.4 Si falta información
No inventar.  
Debe indicarlo claramente y usar la alternativa más prudente posible.

---

## 20. Lo que Codex nunca debe hacer

1. No rehacer módulos enteros sin motivo.
2. No inventar tablas, columnas, vistas ni stored procedures inexistentes.
3. No mezclar varias arquitecturas contradictorias.
4. No meter lógica de negocio pesada dentro de componentes Razor.
5. No reemplazar Dapper por EF sin autorización explícita.
6. No agregar dependencias sin justificar.
7. No cambiar naming o idioma arbitrariamente.
8. No generar código “demo” o académico para problemas de producción.
9. No ocultar cambios estructurales importantes.
10. No asumir que algo puede romperse “después se ve”.

---

## 21. Patrón recomendado de trabajo

Ante cada pedido, Codex debe seguir este orden:

1. Revisar lo existente
2. Detectar qué ya está implementado
3. Detectar faltantes reales
4. Reutilizar lo existente
5. Proponer cambios mínimos necesarios
6. Implementar
7. Explicar concretamente qué hizo

---

## 22. Criterio de calidad esperado

Una implementación se considera correcta si cumple con:

- funciona
- no rompe lo anterior
- es clara
- es mantenible
- respeta arquitectura
- respeta SQL Server + Dapper
- reutiliza
- no agrega complejidad innecesaria

---

## 23. Instrucción operativa fija para cualquier tarea

Antes de modificar código, Codex debe asumir lo siguiente:

- leer la estructura existente
- trabajar sobre la base actual
- no rehacer sin necesidad
- usar Dapper para acceso a datos
- priorizar vistas SQL y consultas claras
- mantener la interfaz en español
- respetar naming existente
- justificar cambios importantes
- entregar archivo por archivo

---

## 24. Prompt corto recomendado para usar junto con este archivo

Usar este texto base al pedir cambios:

> Leé y respetá estrictamente `CODEX_RULES.md`.
> Trabajá sobre la estructura actual del proyecto.
> No rehagas desde cero.
> No rompas funcionalidad existente.
> Reutilizá componentes, servicios y repositorios existentes.
> Usá Dapper para acceso a datos.
> Priorizá SQL Server y vistas existentes.
> Mostrá los cambios archivo por archivo y explicá brevemente cada cambio importante.

---

## 25. Criterio final

Este proyecto no es una demo ni un experimento.
Es una base real para la evolución web de Alfa Gestión.

Toda decisión debe priorizar:

- estabilidad
- claridad
- mantenimiento
- reutilización
- crecimiento ordenado