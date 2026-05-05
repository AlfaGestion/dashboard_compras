# Conversaciones

## Descripción del proyecto

`Conversaciones` es un nuevo módulo de `AlfaCore` orientado a centralizar la gestión de conversaciones de WhatsApp dentro de la aplicación Blazor existente.

La idea funcional es ofrecer una experiencia similar a un inbox operativo:

- ver conversaciones activas
- identificar cliente y contacto
- asignar cada conversación a un técnico
- responder mensajes
- registrar notas internas
- cambiar estados de atención
- conservar trazabilidad operativa

El módulo fue pensado para integrarse con la base actual de Alfa Gestión, reutilizando estructuras ya confirmadas del sistema:

- clientes desde `VT_CLIENTES`
- técnicos/agentes desde `V_TA_Tecnicos`
- contactos desde `MA_CONTACTOS`
- logging técnico centralizado en `AUX_ERR`

## Objetivo funcional

El módulo debe permitir que el equipo atienda WhatsApp desde una única pantalla interna, vinculando cada conversación con personas y entidades reales del sistema.

Objetivos principales:

- concentrar conversaciones entrantes y salientes en un solo lugar
- facilitar la asignación por técnico
- registrar historial completo de mensajes
- preparar la integración formal con WhatsApp Cloud API de Meta
- evitar pérdida de contexto entre contacto, cliente y técnico

## Alcance actual

Hasta este momento quedó preparada la base conceptual y backend inicial del módulo:

- análisis de tablas existentes
- modelo SQL inicial `CONV_*`
- servicio backend `ConversacionesService`
- endpoints internos para inbox, mensajes, asignación, estado y webhook
- configuración base para integración con WhatsApp

Todavía no está implementado en esta etapa:

- pantalla Blazor del inbox
- opción de menú visible
- tiempo real con SignalR
- envío 100% productivo contra Meta
- pruebas funcionales end-to-end

## Arquitectura prevista

### Frontend

- Blazor Server sobre la estructura existente de `AlfaCore`
- pantalla principal tipo inbox en tres paneles:
  - lista de conversaciones
  - chat central
  - panel lateral de contexto

### Backend

- servicios C# dentro del proyecto actual
- acceso a SQL Server con `SqlConnection` y SQL explícito
- logging técnico y auditoría usando servicios ya existentes del sistema

### Base de datos

Modelo nuevo propuesto:

- `CONV_ESTADOS`
- `CONV_CONVERSACIONES`
- `CONV_MENSAJES`
- `CONV_ASIGNACIONES`
- `CONV_ETIQUETAS`
- `CONV_CONVERSACION_ETIQUETAS`
- `CONV_ADJUNTOS`
- `CONV_WEBHOOK_LOG`

### Integraciones existentes reutilizadas

- `VT_CLIENTES`
- `V_TA_Tecnicos`
- `MA_CONTACTOS`
- `TA_USUARIOS`
- `AUX_ERR`

## Criterios funcionales ya definidos

### Técnicos

La asignación principal de una conversación se resuelve con:

- `V_TA_Tecnicos.IdTecnico`

Además, como `V_TA_Tecnicos` ya referencia a `TA_USUARIOS`, el módulo puede resolver auditoría y acciones del usuario real del sistema cuando haga falta.

### Contactos

Se usa únicamente:

- `MA_CONTACTOS`

Esto simplifica el módulo y evita duplicar lógica entre contactos, clientes y contactos varios.

### Clientes

La lectura funcional debe apoyarse en:

- `VT_CLIENTES`

La FK física del modelo nuevo puede seguir apoyándose en:

- `MA_CUENTAS.CODIGO`

porque `VT_CLIENTES` es una vista.

### Estados

No se reutiliza `TA_ESTADOS`, porque ese catálogo corresponde a provincias.

Para conversaciones se propone un catálogo propio:

- `CONV_ESTADOS`

## Endpoints backend preparados

Endpoints internos ya previstos:

- `GET /api/conversaciones`
- `GET /api/conversaciones/{id}`
- `GET /api/conversaciones/{id}/mensajes`
- `POST /api/conversaciones/{id}/mensajes`
- `POST /api/conversaciones/{id}/notas`
- `POST /api/conversaciones/{id}/asignacion`
- `POST /api/conversaciones/{id}/estado`
- `GET /api/conversaciones/whatsapp/webhook`
- `POST /api/conversaciones/whatsapp/webhook`

## Issues pendientes

Por ahora dejo los issues en documento dentro del repo, porque:

- quedan versionados junto al desarrollo
- son fáciles de revisar antes de cada etapa
- permiten moverlos luego a GitHub Issues si más adelante querés formalizar el tablero

### Issue 1. Crear pantalla principal de inbox

- Agregar página Blazor `Conversaciones`
- Diseñar layout de tres paneles
- Integrar filtros base
- Mostrar lista de conversaciones y detalle

### Issue 2. Agregar opción de menú

- Incorporar `Conversaciones` al menú lateral
- Definir ubicación dentro de la navegación actual
- Mantener consistencia visual con el resto del sistema

### Issue 3. Construir panel de lista de conversaciones

- Mostrar técnico asignado, estado, cliente/contacto, teléfono y resumen
- Resaltar conversación activa
- Soportar filtros:
  - Todas
  - Sin asignar
  - Asignadas a mí
  - Pendientes
  - Cerradas

### Issue 4. Construir panel central de chat

- Mostrar historial de mensajes en orden cronológico
- Diferenciar:
  - entrantes
  - salientes
  - notas internas
- Agregar caja de respuesta
- Agregar acción para nota interna

### Issue 5. Construir panel lateral de contexto

- Mostrar datos del contacto
- Mostrar datos del cliente
- Mostrar técnico asignado
- Mostrar estado actual
- Mostrar etiquetas futuras

### Issue 6. Implementar selector de técnico

- Listar técnicos desde `V_TA_Tecnicos`
- Permitir asignar y reasignar
- Registrar historial en `CONV_ASIGNACIONES`

### Issue 7. Implementar gestión de estados

- Alta inicial de estados del módulo
- Cambio de estado desde UI
- Cierre y reapertura de conversación

### Issue 8. Completar flujo de envío WhatsApp

- Confirmar credenciales Meta reales
- Validar formato final del payload
- Manejar respuestas y errores de API
- Persistir `whatsapp_message_id`

### Issue 9. Completar flujo de webhook

- Validar payload real recibido desde Meta
- Confirmar parseo de mensajes, contactos y eventos
- Soportar más tipos de mensaje además de texto

### Issue 10. Agregar soporte de adjuntos

- Persistir metadata en `CONV_ADJUNTOS`
- Mostrar adjuntos en la UI
- Preparar descarga o vista previa

### Issue 11. Preparar tiempo real

- Evaluar incorporación de SignalR
- Actualizar inbox y chat sin refresh manual
- Notificar cambios de asignación y estado

### Issue 12. Pruebas manuales y de integración

- Alta de conversación entrante
- Respuesta saliente
- Cambio de técnico
- Cambio de estado
- Registro correcto en `AUX_ERR` ante fallos

## Próxima etapa sugerida

La etapa 3 debería enfocarse en:

- UI Blazor del módulo
- menú
- integración de servicios backend ya creados

Con eso el equipo ya podría empezar a usar un inbox funcional aunque la integración con Meta todavía esté en fase controlada.
