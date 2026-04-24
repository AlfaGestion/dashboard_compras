# AGENTE.md

Este proyecto utiliza reglas obligatorias definidas en:

## Lectura obligatoria (siempre)

- CODEX_RULES.md
- DATABASE_OBJETOS_SQL_PRIORITARIOS.md

Estas definen:
- cómo trabajar
- qué objetos usar
- reglas críticas del sistema

---

## Lectura opcional (solo si es necesario)

- DATABASE_TABLES_SUMMARY.md

Usar únicamente cuando:
- se necesite entender una tabla específica
- haya dudas sobre la estructura de datos
- no alcance con DATABASE_OBJETOS_SQL_PRIORITARIOS.md

No cargar este archivo completo si no es necesario.

---

## Reglas de trabajo

- Trabajar siempre sobre la base actual
- No rehacer desde cero
- No asumir estructuras no confirmadas
- Priorizar objetos definidos como “oficiales”
- Todo error relevante debe registrarse en `AUX_ERR`, usando un servicio centralizado de logging.
---

## Regla clave

Antes de usar una tabla:

1. Revisar DATABASE_OBJETOS_SQL_PRIORITARIOS.md
2. Si no alcanza → consultar DATABASE_TABLES_SUMMARY.md


