# Review 001 — Emitir comprobante

Revisado por `spec-reviewer` (autorrevisión) el 22/10/2026 con el checklist de `.claude/agents/spec-reviewer.md`.

| # | Ubicación | Hallazgo | Severidad | Corrección aplicada |
|---|---|---|---|---|
| 1 | CA-11 | El resultado no daba el IGV ni el importe: no se podía verificar a mano | Media | Se agregó IGV = 0.05 e importe = 1.85 |
| 2 | RF-05, matriz fila 35 | Código de tributo ICBPER 7152 y tags `BaseUnitMeasure`/`PerUnitAmount` sin fuente primaria abierta | Media | Marcado "por verificar" en research y matriz; pendiente `sunat-expert` |
| 3 | RF-05 (ISC sistema 03) | Fórmula del factor sobre precio de venta al público no confirmada | Media | Marcado "por verificar" en la skill `facturacion-sunat`; no hay CA del sistema 03 hasta confirmarla |
| 4 | RF-07 a RF-10 | Mencionan XSD, XMLDSig y ZIP | Baja | Se mantienen: son formatos exigidos por el enunciado y SUNAT, no elección tecnológica |

Resto del checklist: los 5 patrones EARS presentes; 17 RF con CA; CA negativos presentes; RNF medibles; 8 clarificaciones; términos del glosario; sin carpetas de código.
