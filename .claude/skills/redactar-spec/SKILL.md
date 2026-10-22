---
name: redactar-spec
description: Método para redactar una spec.md spec-first — análisis, glosario, HU, RF con patrones EARS, RNF medibles, CA Dado/Cuando/Entonces, fuera de alcance y clarificaciones.
---

# Redactar una spec

## 1. Analiza antes de escribir
Lee `specs/analisis-enunciado.md`. Si la feature trae información nueva, añade filas (actores, entidades, restricciones, vacíos) citando de dónde sale.

## 2. Glosario primero
Todo sustantivo de negocio que vayas a usar debe estar en `specs/glossary.md`. Si no está, agrégalo con una definición de una línea antes de usarlo.

## 3. Historias de usuario
`HU-NN: Como <rol del glosario>, quiero <acción> para <beneficio>.` Una por objetivo del usuario, no por pantalla.

## 4. Requisitos funcionales (EARS)
Siempre "deberá". Indica el patrón entre paréntesis:

| Patrón | Plantilla | Ejemplo |
|---|---|---|
| Siempre | El sistema deberá … | El sistema deberá firmar el XML UBL antes de enviarlo. |
| Evento | Cuando <evento>, el sistema deberá … | Cuando el Vendedor confirme la emisión, el sistema deberá generar el XML UBL. |
| Estado | Mientras <estado>, el sistema deberá … | Mientras el total de la Boleta supere S/ 700, el sistema deberá exigir documento. |
| No deseado | Si <falla>, entonces el sistema deberá … | Si el XML no valida contra el XSD, entonces el sistema deberá detener el envío. |
| Opcional | Donde <condición>, el sistema deberá … | Donde el Adquirente tenga correo, el sistema deberá enviarle el PDF. |

Prohibido: "podría", "idealmente", "rápido", "fácil", "etc.", nombres de tecnología.

## 5. RNF medibles
Número + unidad + condición: "≤ 2 s en p95 para 50 Ítems", "5 años sin modificación (hash)".

## 6. Criterios de aceptación
```
CA-NN (RF-NN) — <título corto>
  Dado <estado inicial con datos concretos>
  Cuando <acción>
  Entonces <resultado observable>
```
- Datos concretos (montos, RUC, fechas), no "un valor válido".
- Al menos un CA negativo por RF que pueda fallar.
- Para cálculos, que el resultado se pueda verificar a mano.

## 7. Fuera de alcance
Lista explícita de lo que la feature NO hace, para cerrar discusiones.

## 8. Clarificaciones
Mínimo 3, cada una con una pregunta cerrada que un humano pueda contestar: `[NEEDS CLARIFICATION] ¿…?`. Salen de los vacíos del análisis, no se inventan.

## 9. Cierre
- Actualiza `specs/traceability/trazabilidad.md` (una fila por CA).
- Si el RF toca normativa, verifica que tenga fila en `research.md` y en `matriz-normativa.md`.
- Pide revisión al agente `spec-reviewer`.
