Actualizaciones SQL versionadas por nombre.

Propuesta de uso:
- mantener estos scripts en el repo como fuente oficial
- copiar los `.sql` publicados a una carpeta compartida de red, por ejemplo `\\SERVIDOR\AlfaGestion\UpdatesSQL\`
- ejecutar cada script una sola vez por base, controlando el avance con `TA_CONFIGURACION.CLAVE = FECHAUPDATE`

Convención sugerida:
- `AAAA-MM-DD-NNN__descripcion.sql`

Ejemplo:
- `2026-05-14-001__interfaces_tipos_documento_controles_arca.sql`

Reglas:
- cada script debe ser idempotente
- cada script debe poder reintentarse sin romper datos
- `FECHAUPDATE` debe guardar la versión textual completa, por ejemplo `2026-05-14-001`
- el prefijo `NNN` permite varias actualizaciones el mismo día
- si una base todavía tiene un `FECHAUPDATE` viejo en formato solo fecha, el sistema lo interpreta como una versión legada de ese día
