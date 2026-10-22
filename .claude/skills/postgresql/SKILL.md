---
name: postgresql
description: Buenas prácticas de PostgreSQL para persistir documentos electrónicos, correlativos, movimientos migrados y registros, alineadas a la constitución del proyecto.
---

# PostgreSQL en este proyecto

1. **Correlativo atómico.** `UPDATE serie SET ultimo_correlativo = ultimo_correlativo + 1 WHERE codigo = $1 RETURNING ultimo_correlativo;` dentro de la misma transacción que inserta el documento. Nunca `MAX()+1` (CA-01, CA-02). Índice único `(ruc_emisor, tipo, serie, correlativo)`.

2. **Dinero en `numeric(14,2)`** y valores unitarios en `numeric(22,10)`. Nunca `real`/`double precision`.

3. **Inmutabilidad.** Documentos emitidos sin `UPDATE` de campos de negocio: trigger que rechace cambios salvo columnas de estado y CDR; el XML firmado se guarda con su SHA-256 (`bytea` o archivo + hash).

4. **Trazabilidad del legado.** Tabla `movimiento_inventario` con `lote_id`, `fila_origen`, `contenido_original jsonb` y `hash_fila` con índice único para deduplicar (RF-30, RF-33).

5. **Catálogos con vigencia.** `catalogo_sunat(numero, codigo, descripcion, vigente_desde, vigente_hasta)`; consultas por fecha de emisión, no por "último".
