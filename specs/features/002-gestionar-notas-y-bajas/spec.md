# Spec 002 — Gestionar notas, bajas y resumen diario

## Contexto
Un Comprobante emitido es inmutable; los errores, devoluciones y descuentos se corrigen con Notas o con Comunicación de baja. Las Boletas se informan a SUNAT por Resumen diario. Ambos procesos tienen plazos y códigos propios (catálogos 09 y 10).

## Actores
- **Vendedor**, **Administrador**, **Validador SUNAT** (definidos en el glosario).

## Historias de usuario
- HU-07: Como Vendedor, quiero emitir una nota de crédito por devolución o descuento para corregir un Comprobante sin anularlo a mano.
- HU-08: Como Vendedor, quiero emitir una nota de débito por intereses o aumento de valor.
- HU-09: Como Administrador, quiero dar de baja un Comprobante emitido por error, dentro del plazo.
- HU-10: Como Administrador, quiero que las Boletas del día se informen solas en un Resumen diario y ver su resultado.
- HU-11: Como Vendedor, quiero consultar el Estado SUNAT de cualquier Documento electrónico y reenviar los pendientes.

## Requisitos funcionales
- **RF-18** (Siempre): El sistema deberá exigir en toda Nota el Documento electrónico que modifica (tipo, Serie y Correlativo) y un motivo del catálogo 09 (nota de crédito) o 10 (nota de débito).
- **RF-19** (No deseado): Si la Nota hace referencia a un Comprobante que no está ACEPTADO ni OBSERVADO, entonces el sistema deberá bloquear la emisión.
- **RF-20** (Siempre): El sistema deberá asignar a la Nota una Serie cuya primera letra coincide con la del Comprobante modificado (F para factura, B para boleta).
- **RF-21** (Siempre): El sistema deberá permitir que cada Nota modifique un único Comprobante.
- **RF-22** (No deseado): Si la suma de notas de crédito sobre un Comprobante supera su importe total, entonces el sistema deberá bloquear la nueva nota.
- **RF-23** (Evento): Cuando el Administrador solicite la baja de un Documento electrónico, el sistema deberá incluirlo en una Comunicación de baja con el motivo, enviarla y consultar el Ticket hasta obtener el CDR.
- **RF-24** (No deseado): Si la baja se solicita después de 7 días calendario contados desde el día siguiente a la emisión, entonces el sistema deberá rechazarla e indicar que corresponde nota de crédito.
- **RF-25** (Evento): Cuando termine el día de emisión, el sistema deberá generar el Resumen diario con las Boletas y sus Notas no informadas, en bloques de hasta 500 líneas, enviarlo y consultar su Ticket.
- **RF-26** (Estado): Mientras un Resumen diario esté sin CDR, el sistema deberá consultar su Ticket periódicamente y mostrar el estado "en proceso".
- **RF-27** (Evento): Cuando el Vendedor consulte un Documento electrónico, el sistema deberá mostrar su Estado SUNAT, el código y la descripción del CDR y permitir reenviar los que estén en PENDIENTE_REENVIO.

## Requisitos no funcionales
- **RNF-08**: El 100 % de las Boletas de un día deberá quedar informado dentro de los 7 días calendario siguientes.
- **RNF-09**: La consulta de un Ticket deberá reintentarse cada 5 minutos como máximo, hasta 24 h.

## Criterios de aceptación
CA-28 (RF-18) — Nota sin motivo
  Dada una nota de crédito sin código de catálogo 09
  Cuando el Vendedor intenta emitirla
  Entonces el sistema la bloquea con "Motivo obligatorio"

CA-29 (RF-18) — Devolución por ítem
  Dada la Factura ACEPTADA F001-00000042 con 10 unidades de P-001
  Cuando el Vendedor emite nota de crédito motivo 07 por 3 unidades
  Entonces la Nota referencia a F001-00000042 y su importe corresponde a 3 unidades

CA-30 (RF-19) — Nota sobre comprobante rechazado
  Dado el Comprobante F001-00000043 en estado RECHAZADO
  Cuando el Vendedor intenta emitir una nota de crédito sobre él
  Entonces el sistema la bloquea

CA-31 (RF-20) — Serie de la nota
  Dada una nota de crédito sobre la Boleta B001-00000010
  Cuando se asigna la Serie
  Entonces la Serie empieza con B

CA-32 (RF-21) — Nota sobre dos comprobantes
  Dada una nota de crédito que referencia F001-00000042 y F001-00000044
  Cuando el Vendedor intenta emitir
  Entonces el sistema la bloquea con "Una nota modifica un solo comprobante"

CA-33 (RF-22) — Exceso de crédito
  Dado F001-00000042 por 118.00 con una nota de crédito previa de 100.00
  Cuando el Vendedor emite otra nota de crédito por 20.00
  Entonces el sistema la bloquea

CA-34 (RF-23) — Baja aceptada
  Dada la Factura F001-00000045 emitida ayer
  Cuando el Administrador la da de baja con motivo "Error en descripción"
  Entonces se envía una Comunicación de baja, se obtiene Ticket y al recibir CDR aceptado queda DADO_DE_BAJA

CA-35 (RF-24) — Baja fuera de plazo
  Dada una Factura emitida hace 9 días calendario
  Cuando el Administrador pide su baja
  Entonces el sistema la rechaza indicando "Use nota de crédito"

CA-36 (RF-25) — Resumen de 620 boletas
  Dadas 620 Boletas emitidas el 21/10/2026
  Cuando se genera el Resumen diario
  Entonces se crean 2 envíos: uno de 500 líneas y otro de 120

CA-37 (RF-26) — Ticket en proceso
  Dado un Resumen diario cuyo Ticket responde "en proceso"
  Cuando el Vendedor abre el listado
  Entonces el Resumen muestra "en proceso" y hay una consulta programada

CA-38 (RF-27) — Reenvío manual
  Dado un Comprobante en PENDIENTE_REENVIO
  Cuando el Vendedor pulsa reenviar y el Validador SUNAT responde código 0
  Entonces el Estado SUNAT pasa a ACEPTADO

## Fuera de alcance
- Notas por ajustes de exportación (motivo 13 de exportación).
- Reversiones de retención y percepción.

## Clarificaciones
- [NEEDS CLARIFICATION] ¿Quién puede emitir notas y bajas? ¿El Vendedor o solo el Administrador?
- [NEEDS CLARIFICATION] ¿ABC enviará Boletas por Resumen diario o individualmente? (la norma permite ambas)
- [NEEDS CLARIFICATION] ¿Las devoluciones físicas reingresan al inventario automáticamente?
- [NEEDS CLARIFICATION] La regla "una nota, un comprobante" rige en SEE-OSE desde el 1/1/2027 (RS 000048-2026); se adopta ya. ¿Lo aprueba la gerencia?
