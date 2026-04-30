from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path


TITLE = "# Catálogo de rutinas - Alfa Gestión"
REQUIRED_FIELDS = ["Tipo", "Ubicación", "Propósito", "Datos que usa", "Observaciones"]
ALLOWED_TYPES = {"Service", "Repository", "DTO", "Page", "View", "SP"}
LOCAL_CODE_TYPES = {"Service", "Repository", "DTO", "Page"}
AMBIGUOUS_SQL_PREFIXES = ("MA_", "MV_", "TA_", "C_", "V_", "P_", "AUX_", "AC_", "S_")
LOCAL_PATH_RE = re.compile(r"`((?:src|docs|tools|scripts|installer|publish|App_Data)/[^`]+)`")
FIELD_RE = re.compile(r"^- ([^:]+):\s*(.+?)\s*$")


@dataclass
class RoutineEntry:
    module: str
    name: str
    line: int
    fields: dict[str, str] = field(default_factory=dict)
    duplicate_fields: list[str] = field(default_factory=list)


def normalize_sql_name(name: str) -> str:
    return name.strip().strip("`").strip()


def is_ambiguous_sql_name(name: str) -> bool:
    upper = normalize_sql_name(name).upper()
    if upper.startswith("VW_") or upper.startswith("SP_"):
        return False
    return any(upper.startswith(prefix) for prefix in AMBIGUOUS_SQL_PREFIXES)


def parse_catalog(text: str) -> tuple[list[str], list[RoutineEntry]]:
    modules: list[str] = []
    routines: list[RoutineEntry] = []
    current_module: str | None = None
    current_routine: RoutineEntry | None = None

    for idx, raw_line in enumerate(text.splitlines(), start=1):
        line = raw_line.rstrip()

        if line.startswith("## "):
            if current_routine is not None:
                routines.append(current_routine)
                current_routine = None
            current_module = line[3:].strip()
            modules.append(current_module)
            continue

        if line.startswith("### "):
            if current_routine is not None:
                routines.append(current_routine)
            current_routine = RoutineEntry(
                module=current_module or "(sin módulo)",
                name=line[4:].strip(),
                line=idx,
            )
            continue

        if current_routine is None:
            continue

        match = FIELD_RE.match(line)
        if not match:
            continue

        key = match.group(1).strip()
        value = match.group(2).strip()

        if key in current_routine.fields:
            current_routine.duplicate_fields.append(key)
        else:
            current_routine.fields[key] = value

    if current_routine is not None:
        routines.append(current_routine)

    return modules, routines


def find_existing_paths(root: Path, value: str) -> list[Path]:
    paths: list[Path] = []
    for rel in LOCAL_PATH_RE.findall(value):
        path = root / Path(rel)
        if path.exists():
            paths.append(path)
    return paths


def object_is_referenced(root: Path, object_name: str) -> bool:
    needle = normalize_sql_name(object_name)
    if not needle:
        return False

    for base in (root / "src", root / "docs"):
        if not base.exists():
            continue
        for path in base.rglob("*"):
            if not path.is_file():
                continue
            if path.suffix.lower() not in {".cs", ".razor", ".md", ".sql", ".json", ".py"}:
                continue
            try:
                content = path.read_text(encoding="utf-8")
            except UnicodeDecodeError:
                try:
                    content = path.read_text(encoding="latin-1")
                except UnicodeDecodeError:
                    continue
            if needle.lower() in content.lower():
                return True
    return False


def validate_catalog(root: Path, catalog_path: Path) -> tuple[list[str], list[str], list[RoutineEntry]]:
    errors: list[str] = []
    warnings: list[str] = []

    if not catalog_path.exists():
        return [f"No existe el archivo requerido: {catalog_path}"], warnings, []

    text = catalog_path.read_text(encoding="utf-8")
    if not text.lstrip().startswith(TITLE):
        errors.append(f"El catálogo debe comenzar con el título exacto: {TITLE}")

    modules, routines = parse_catalog(text)

    if not modules:
        errors.append("El catálogo no contiene módulos (`## ...`).")
    if not routines:
        errors.append("El catálogo no contiene rutinas (`### ...`).")

    seen_names: dict[str, list[str]] = {}

    for routine in routines:
        seen_names.setdefault(routine.name.lower(), []).append(routine.module)

        if routine.module == "(sin módulo)":
            errors.append(f"Línea {routine.line}: la rutina '{routine.name}' quedó fuera de un módulo.")

        for dup in routine.duplicate_fields:
            errors.append(
                f"Línea {routine.line}: la rutina '{routine.name}' repite el campo '{dup}'."
            )

        for field_name in REQUIRED_FIELDS:
            if field_name not in routine.fields:
                errors.append(
                    f"Línea {routine.line}: la rutina '{routine.name}' no tiene el campo obligatorio '{field_name}'."
                )

        extra_fields = set(routine.fields) - set(REQUIRED_FIELDS)
        for extra in sorted(extra_fields):
            warnings.append(
                f"Línea {routine.line}: la rutina '{routine.name}' tiene un campo adicional no validado: '{extra}'."
            )

        tipo = routine.fields.get("Tipo", "").strip()
        if tipo and tipo not in ALLOWED_TYPES:
            errors.append(
                f"Línea {routine.line}: la rutina '{routine.name}' usa un tipo no permitido: '{tipo}'."
            )

        ubicacion = routine.fields.get("Ubicación", "").strip()
        local_paths = find_existing_paths(root, ubicacion)

        if tipo in LOCAL_CODE_TYPES:
            if not local_paths:
                errors.append(
                    f"Línea {routine.line}: la rutina '{routine.name}' debe apuntar a una ruta local existente en 'Ubicación'."
                )
            elif tipo == "Page" and not any(path.suffix.lower() == ".razor" for path in local_paths):
                errors.append(
                    f"Línea {routine.line}: la página '{routine.name}' debe referenciar al menos un archivo '.razor'."
                )
            elif tipo in {"Service", "Repository", "DTO"} and not any(path.suffix.lower() == ".cs" for path in local_paths):
                errors.append(
                    f"Línea {routine.line}: la rutina '{routine.name}' debe referenciar al menos un archivo '.cs'."
                )

        if tipo in {"View", "SP"}:
            sql_name = normalize_sql_name(routine.name)
            if not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", sql_name):
                errors.append(
                    f"Línea {routine.line}: el nombre del objeto SQL '{routine.name}' no tiene un formato válido."
                )
            if is_ambiguous_sql_name(sql_name):
                errors.append(
                    f"Línea {routine.line}: '{routine.name}' tiene prefijo ambiguo; no se debe asumir tipo técnico '{tipo}' sin verificación en SQL Server."
                )
            if not object_is_referenced(root, sql_name):
                errors.append(
                    f"Línea {routine.line}: el objeto SQL '{routine.name}' no aparece referenciado en `src/` o `docs/`."
                )

        for label in ("Propósito", "Datos que usa", "Observaciones"):
            value = routine.fields.get(label, "").strip()
            if not value:
                errors.append(
                    f"Línea {routine.line}: la rutina '{routine.name}' tiene vacío el campo '{label}'."
                )

    for name, module_list in sorted(seen_names.items()):
        unique_modules = sorted(set(module_list))
        if len(unique_modules) > 1:
            warnings.append(
                f"La rutina '{name}' aparece en más de un módulo: {', '.join(unique_modules)}."
            )

    return errors, warnings, routines


def main() -> int:
    default_root = Path(__file__).resolve().parents[2]

    parser = argparse.ArgumentParser(
        description="Verifica la estructura y consistencia básica de docs/CATALOGO_RUTINAS.md."
    )
    parser.add_argument(
        "--root",
        type=Path,
        default=default_root,
        help="Raíz del repositorio. Por defecto usa la carpeta del script.",
    )
    parser.add_argument(
        "--catalog",
        type=Path,
        default=None,
        help="Ruta al catálogo a verificar. Por defecto usa docs/CATALOGO_RUTINAS.md dentro de --root.",
    )
    args = parser.parse_args()

    root = args.root.resolve()
    catalog_path = args.catalog.resolve() if args.catalog else root / "docs" / "CATALOGO_RUTINAS.md"

    errors, warnings, routines = validate_catalog(root, catalog_path)

    print(f"Catálogo: {catalog_path}")
    print(f"Rutinas relevadas: {len(routines)}")
    print(f"Advertencias: {len(warnings)}")
    print(f"Errores: {len(errors)}")

    if warnings:
        print("\nAdvertencias:")
        for warning in warnings:
            print(f"- {warning}")

    if errors:
        print("\nErrores:")
        for error in errors:
            print(f"- {error}")
        return 1

    print("\nOK: el catálogo cumple las validaciones básicas definidas por el proyecto.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
