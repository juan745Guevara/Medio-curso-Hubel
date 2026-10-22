# Research 002 — Notas, bajas y resumen diario

| Tema | Qué exige | Fuente | RF |
|---|---|---|---|
| Catálogo 09 (NC) | 01 anulación, 02 anulación por error en RUC, 03 corrección descripción, 04 descuento global, 05 descuento por ítem, 06 devolución total, 07 devolución por ítem, 08 bonificación, 09 disminución en el valor, 10 otros, 13 ajustes de montos/fechas de pago. | [Trámites Perú](https://tramitesperu.com/sunat/notas-credito-debito-electronicas/), [SUNAT informe 057-2025](https://www.sunat.gob.pe/legislacion/oficios/2025/informe-oficios/i000057-2025-7T0000.pdf) | RF-18 |
| Catálogo 10 (ND) | 01 intereses por mora, 02 aumento en el valor, 03 penalidades/otros. | [Anexo E RS 245-2017](https://www.sunat.gob.pe/legislacion/superin/2017/anexoE-245-2017.pdf) | RF-18 |
| Nota 1 a 1 | En SEE-OSE una nota pasa a modificar un solo comprobante; en SEE-Del contribuyente ya regía. Vigencia 1/1/2027. | [Sovos](https://sovos.com/es/cambios-regulatorios/iva/peru-boleta-venta-electronica-rs-000048-2026-sunat/) | RF-21 |
| Plazo de baja | 7 días calendario desde el día siguiente a la generación (unificado desde 2027). | [Sovos](https://sovos.com/es/cambios-regulatorios/iva/peru-boleta-venta-electronica-rs-000048-2026-sunat/), [SUNAT CPE](https://cpe.sunat.gob.pe/noticias/comprobantes-desde-los-sistemas-del-contribuyente) | RF-24 |
| Resumen diario | Hasta el 7.º día calendario siguiente; bloques de 500 líneas; `sendSummary` → ticket → `getStatus`. | [SUNAT orientación](https://orientacion.sunat.gob.pe/04-resumen-diario-boleta-de-venta-electronica), [Anexo 244-2019](https://ww3.sunat.gob.pe/legislacion/superin/2019/anexo-244-2019.pdf) | RF-25 |
| Formato de resumen y baja | `SummaryDocuments` y `VoidedDocuments` siguen en UBL 2.0. | [esolutions/xml](https://packagist.org/packages/esolutions/xml) | RF-23, RF-25 |
