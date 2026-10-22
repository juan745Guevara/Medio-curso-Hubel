# Spec 004 — Generar registro de ventas y kardex

## Contexto
La RS 234-2006/SUNAT exige llevar libros y registros formales. Con los Documentos electrónicos y los Movimientos de inventario del sistema, ABC puede producir el Registro de ventas del Periodo y el kardex por Producto, que hoy no lleva.

## Actores
- **Contador**: exporta y revisa registros.
- **Administrador**.

## Historias de usuario
- HU-15: Como Contador, quiero el Registro de ventas del Periodo con todos los Documentos electrónicos para cuadrar con SUNAT.
- HU-16: Como Administrador, quiero el kardex por Producto (entradas, salidas, saldo) desde el primer movimiento migrado.

## Requisitos funcionales
- **RF-37** (Evento): Cuando el Contador solicite el Registro de ventas de un Periodo, el sistema deberá listar cada Documento electrónico del Periodo con fecha, tipo, Serie, Correlativo, Documento de identidad del Adquirente, base por Afectación IGV, IGV, ISC, ICBPER, importe total y Estado SUNAT.
- **RF-38** (Siempre): El sistema deberá restar en el Registro de ventas las notas de crédito y sumar las notas de débito del Periodo en que se emiten.
- **RF-39** (No deseado): Si un Documento electrónico del Periodo está en PENDIENTE_REENVIO o RECHAZADO, entonces el sistema deberá marcarlo en el Registro y excluirlo de los totales.
- **RF-40** (Siempre): El sistema deberá mostrar el kardex de un Producto con cada Movimiento de inventario (fecha, tipo, cantidad, documento de soporte, saldo acumulado), incluidos los migrados.
- **RF-41** (Evento): Cuando el Contador exporte el Registro de ventas, el sistema deberá generar un archivo con la estructura que se defina en la clarificación correspondiente.

## Requisitos no funcionales
- **RNF-12**: El Registro de ventas de un Periodo con 20 000 Documentos electrónicos deberá generarse en ≤ 30 s.

## Criterios de aceptación
CA-48 (RF-37) — Registro del periodo
  Dados 3 Comprobantes ACEPTADOS en 2026-10
  Cuando el Contador pide el Registro de 2026-10
  Entonces ve 3 filas con todos los campos de RF-37

CA-49 (RF-38) — Nota de crédito resta
  Dada una Factura de 118.00 y una nota de crédito de 18.00 en el mismo Periodo
  Cuando se generan totales
  Entonces el importe neto es 100.00

CA-50 (RF-39) — Rechazado excluido
  Dado un Comprobante RECHAZADO en el Periodo
  Cuando se genera el Registro
  Entonces aparece marcado y no suma

CA-51 (RF-40) — Kardex con migrados
  Dado el Producto P-001 con 50 de entrada migrada y 10 de salida por Comprobante
  Cuando el Administrador abre su kardex
  Entonces ve 2 movimientos y saldo final 40

CA-52 (RF-41) — Exportación
  Dado el Registro de 2026-10
  Cuando el Contador exporta
  Entonces obtiene un archivo con una fila por Documento electrónico

## Fuera de alcance
- Presentación del registro en SIRE o PLE.
- Registro de compras y libros contables.

## Clarificaciones
- [NEEDS CLARIFICATION] ¿Qué régimen tributario tiene ABC y qué libros está obligado a llevar según la RS 234-2006?
- [NEEDS CLARIFICATION] ¿El Registro de ventas debe exportarse con la estructura del SIRE (RVIE) o basta un reporte interno?
- [NEEDS CLARIFICATION] ¿El kardex debe ser valorizado (costo promedio) o solo en unidades? El legado no tiene costos.
