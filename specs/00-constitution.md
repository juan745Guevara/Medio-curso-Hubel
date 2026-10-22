# Constitución del sistema

Invariantes que aplican a todo el sistema. Ninguna spec, ADR o tarea puede contradecirlos; cambiarlos exige un ADR nuevo.

## Proceso
1. Ninguna funcionalidad se implementa sin `spec.md` aprobada y sin su fila en `traceability/trazabilidad.md`.
2. Toda regla tributaria cita su fuente (norma, anexo o catálogo) en `research.md` o en `traceability/matriz-normativa.md`.
3. Lo que no está en el enunciado ni en una fuente citada se marca `[NEEDS CLARIFICATION]`; nunca se asume.

## Dominio tributario
4. Ningún comprobante electrónico se envía a SUNAT ni al OSE sin pasar antes, en este orden: validación de negocio, validación contra el XSD UBL 2.1 y firma digital.
5. Un comprobante emitido (con número asignado) es inmutable. Solo se corrige con nota de crédito, nota de débito o comunicación de baja.
6. Un correlativo asignado nunca se reutiliza, aunque el comprobante sea rechazado o dado de baja.
7. Todo importe se calcula y almacena en el dominio con precisión decimal exacta; nunca en coma flotante.
8. La factura solo se emite a un adquirente identificado con RUC válido.
9. Los códigos de catálogo usados en el comprobante provienen de la versión vigente de los catálogos del Anexo 8 cargada en el sistema, con fecha de vigencia.
10. El XML firmado, el CDR y la representación impresa se conservan sin alteración por al menos 5 años.

## Datos y legado
11. Todo registro migrado conserva el identificador y el contenido de su fila de origen (trazabilidad hacia el legado).
12. Un movimiento histórico del legado nunca se convierte en comprobante electrónico emitido.

## Arquitectura
13. El dominio no depende de frameworks, base de datos, interfaz ni del proveedor SUNAT/OSE. Las dependencias apuntan hacia el dominio.
14. Toda comunicación con sistemas externos pasa por un puerto declarado en `contracts/puertos.md`.

## Seguridad
15. Ningún secreto (certificado, contraseña del certificado, clave SOL, token del OSE) se versiona en el repositorio.
16. Las respuestas de error al usuario nunca exponen trazas internas; sí exponen el código y la descripción de SUNAT/OSE cuando existe.
