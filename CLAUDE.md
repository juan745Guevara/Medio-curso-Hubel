# Instrucciones para agentes — proyecto Facturación ABC (Spec-First)

## Fase actual: especificación. No se escribe código.
No crees `backend/`, `frontend/`, `src/` ni archivos de código. Solo se editan `specs/`, `.claude/` y los archivos raíz.

## Flujo obligatorio
1. Enunciado → `specs/analisis-enunciado.md`
2. `spec-writer` → `specs/features/00N-*/spec.md` (skill `redactar-spec`)
3. `sunat-expert` → `research.md` y `specs/traceability/matriz-normativa.md` con fuentes
4. `spec-reviewer` → `review.md`; las clarificaciones van al humano
5. `architect` → ADR, `specs/architecture/`, `specs/domain/`
6. `migration-analyst` → `specs/migration/`
7. `contract-designer` → `specs/contracts/` (valida JSON y YAML)
8. `qa` → un caso de prueba por CA y `specs/traceability/trazabilidad.md`

## Decisiones vigentes
- Arquitectura: monolito MVC con núcleo hexagonal — [ADR 0001](specs/adr/0001-arquitectura.md).
- Stack: .NET 10 (ASP.NET Core MVC + MAUI) + PostgreSQL — [ADR 0002](specs/adr/0002-stack-tecnologico.md).
- XSD antes de firmar — [ADR 0003](specs/adr/0003-validar-xsd-antes-de-firmar.md).
- MSI + APK — [ADR 0004](specs/adr/0004-empaquetado-msi-apk.md).

## Reglas para todos
- Lee `specs/00-constitution.md` y `specs/glossary.md` antes de cualquier tarea.
- Si algo no está en la spec, no lo inventes: pregunta o marca `[NEEDS CLARIFICATION]`.
- Todo dato tributario lleva fuente (URL) o la marca "por verificar".
- Numeración global continua: RF, RNF, CA y HU no se reinician por feature.
- Al cambiar un RF, actualiza en el mismo cambio: CA, `trazabilidad.md`, matriz normativa (si aplica), schema y OpenAPI.
