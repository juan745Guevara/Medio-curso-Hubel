# Spec 001 — Emitir comprobante electrónico

## Contexto
ABC Software Corp vende abarrotes al por mayor y menor y hoy no puede emitir comprobantes válidos: no valida RUC, no calcula IGV, ISC ni ICBPER, no genera XML UBL, no firma y no se comunica con SUNAT ni con un OSE. La RS 097-2012/SUNAT y sus modificatorias le exigen emitir Factura y Boleta electrónicas en XML UBL 2.1 firmado y validado.

## Actores
- **Vendedor**: registra la venta y emite el Comprobante.
- **Administrador**: configura Emisor, Series, Certificado digital, Productos y Validador SUNAT.
- **Validador SUNAT**: SUNAT u OSE; recibe el XML UBL y devuelve el CDR.
- **Adquirente**: recibe la Representación impresa y el XML.

## Historias de usuario
- HU-01: Como Vendedor, quiero emitir una Factura a un cliente con RUC para que pueda sustentar crédito fiscal.
- HU-02: Como Vendedor, quiero emitir una Boleta a un consumidor final para cumplir la obligación de entregar comprobante.
- HU-03: Como Vendedor, quiero que el sistema calcule IGV, ISC e ICBPER por Ítem para no equivocarme en los totales.
- HU-04: Como Vendedor, quiero saber al instante si SUNAT u OSE aceptó o rechazó el Comprobante y por qué, para corregir a tiempo.
- HU-05: Como Vendedor, quiero entregar al cliente el PDF con QR y el XML para que pueda verificarlo.
- HU-06: Como Administrador, quiero configurar Series, Certificado digital y Validador SUNAT sin tocar el código, para operar en pruebas y en producción.

## Requisitos funcionales
- **RF-01** (Siempre): El sistema deberá asignar a cada Comprobante una Serie y un Correlativo únicos, consecutivos por Serie, con formato `SSSS-NNNNNNNN`, donde la Serie de Factura empieza con F y la de Boleta con B.
- **RF-02** (Evento): Cuando el Vendedor ingrese un Documento de identidad tipo 6, el sistema deberá validar que el RUC tenga 11 dígitos, prefijo 10, 15, 16, 17 o 20 y dígito verificador correcto por módulo 11 antes de permitir emitir.
- **RF-03** (No deseado): Si el Vendedor intenta emitir una Factura a un Adquirente sin RUC válido, entonces el sistema deberá bloquear la emisión e indicar que la Factura requiere RUC.
- **RF-04** (Estado): Mientras el importe total de una Boleta supere S/ 700.00, el sistema deberá exigir tipo, número de Documento de identidad y nombre del Adquirente.
- **RF-05** (Siempre): El sistema deberá calcular por Ítem el valor de venta, el ISC según el Sistema ISC del Producto (catálogo 08), el IGV sobre valor de venta más ISC para Ítems gravados (catálogo 07 código 10), y el ICBPER por unidad de bolsa fuera de la base del IGV.
- **RF-06** (Siempre): El sistema deberá calcular los totales del Comprobante: total por Afectación IGV (gravado, exonerado, inafecto), total IGV, total ISC, total ICBPER, total descuentos e importe total, redondeados a 2 decimales.
- **RF-07** (Evento): Cuando el Vendedor confirme la emisión, el sistema deberá generar el XML UBL 2.1 con `UBLVersionID` 2.1 y `CustomizationID` 2.0, Tipo de operación del catálogo 51 y los datos obligatorios del Anexo 9-A.
- **RF-08** (No deseado): Si el XML UBL no valida contra el XSD de OASIS, entonces el sistema deberá detener el proceso, no firmar ni enviar, y registrar el error de estructura.
- **RF-09** (Siempre): El sistema deberá firmar el XML UBL con el Certificado digital del Emisor mediante XMLDSig enveloped dentro de `ext:UBLExtensions` antes de enviarlo.
- **RF-10** (Evento): Cuando un Comprobante quede FIRMADO, el sistema deberá enviarlo al Validador SUNAT configurado (SUNAT u OSE) en un ZIP llamado `RUC-TT-SERIE-CORRELATIVO`, y registrar el CDR con su código y descripción.
- **RF-11** (No deseado): Si el Validador SUNAT no responde o hay falla de red, entonces el sistema deberá dejar el Comprobante en PENDIENTE_REENVIO, reintentar automáticamente y alertar cuando falte 1 día para vencer el plazo de envío.
- **RF-12** (Siempre): El sistema deberá registrar la Forma de pago de la Factura: Contado, o Crédito con Monto neto pendiente y cada Cuota con monto y fecha de vencimiento.
- **RF-13** (Evento): Cuando el Comprobante sea ACEPTADO u OBSERVADO, el sistema deberá generar la Representación impresa con el código QR y permitir descargarla junto con el XML y el CDR.
- **RF-14** (Opcional): Donde el Adquirente tenga correo electrónico registrado, el sistema deberá enviarle la Representación impresa y el XML al quedar ACEPTADO.
- **RF-15** (Siempre): El sistema deberá incluir en el XML UBL la leyenda de importe total en letras (catálogo 52, código 1000).
- **RF-16** (Opcional): Donde el Administrador haya activado Contingencia por falla documentada, el sistema deberá permitir registrar Comprobantes físicos de contingencia y generar su informe para SUNAT dentro de 7 días calendario.
- **RF-17** (Siempre): El sistema deberá descontar del inventario la cantidad de cada Ítem de un Comprobante ACEPTADO u OBSERVADO como Movimiento de inventario de salida enlazado al Comprobante.

## Requisitos no funcionales
- **RNF-01**: La emisión de una Factura de hasta 50 Ítems, desde confirmar hasta tener el XML firmado, deberá tardar ≤ 2 s en el percentil 95.
- **RNF-02**: El 100 % de los XML enviados deberá haber pasado la validación XSD (verificable en el registro de auditoría).
- **RNF-03**: Las Facturas deberán enviarse al Validador SUNAT dentro del plazo vigente (hasta 3 días calendario desde el día siguiente a la emisión); el sistema deberá reportar 0 Facturas vencidas sin alerta previa.
- **RNF-04**: La aplicación deberá operar en Windows 10 o superior instalada por MSI y en Android 10 o superior instalada por APK.
- **RNF-05**: El Certificado digital y su contraseña deberán guardarse cifrados y nunca aparecer en registros ni respuestas.
- **RNF-06**: XML, CDR y Representación impresa deberán conservarse al menos 5 años sin modificación (hash verificable).
- **RNF-07**: Cambiar entre SUNAT y OSE, o entre beta y producción, deberá ser un cambio de configuración sin recompilar.

## Criterios de aceptación
CA-01 (RF-01) — Correlativo consecutivo por Serie
  Dado que la última Factura de la Serie F001 es F001-00000041
  Cuando el Vendedor emite una nueva Factura en F001
  Entonces el Comprobante recibe F001-00000042

CA-02 (RF-01) — Correlativo no se reutiliza
  Dado que F001-00000042 fue RECHAZADO por el Validador SUNAT
  Cuando el Vendedor emite otra Factura en F001
  Entonces recibe F001-00000043 y F001-00000042 queda registrado como RECHAZADO

CA-03 (RF-02) — RUC válido
  Dado el RUC 20131312955
  Cuando el Vendedor lo ingresa como Documento de identidad tipo 6
  Entonces el sistema lo acepta como válido

CA-04 (RF-02) — RUC con dígito verificador incorrecto
  Dado el RUC 20131312954
  Cuando el Vendedor lo ingresa
  Entonces el sistema lo rechaza con el mensaje "RUC inválido: dígito verificador"

CA-05 (RF-03) — Factura sin RUC
  Dado un Adquirente identificado con DNI 45678912
  Cuando el Vendedor intenta emitir una Factura
  Entonces el sistema bloquea la emisión con "La factura requiere RUC del adquirente"

CA-06 (RF-04) — Boleta mayor a S/ 700 sin documento
  Dada una Boleta con importe total S/ 767.00 y sin Documento de identidad
  Cuando el Vendedor intenta emitir
  Entonces el sistema exige tipo y número de documento y nombre

CA-07 (RF-04) — Boleta menor a S/ 700 sin documento
  Dada una Boleta con importe total S/ 85.50 sin Documento de identidad
  Cuando el Vendedor emite
  Entonces el Comprobante se emite con Adquirente genérico

CA-08 (RF-05) — IGV de Ítem gravado
  Dado un Ítem gravado (catálogo 07 = 10) de 10 unidades con Valor unitario S/ 5.00 y sin ISC
  Cuando el sistema calcula
  Entonces valor de venta = 50.00, IGV = 9.00 e importe del Ítem = 59.00

CA-09 (RF-05) — ISC al valor antes del IGV
  Dado un Ítem gravado con valor de venta S/ 100.00, Sistema ISC 01 y tasa ISC configurada en el Producto de 25 %
  Cuando el sistema calcula
  Entonces ISC = 25.00, base IGV = 125.00, IGV = 22.50 e importe del Ítem = 147.50

CA-10 (RF-05) — ISC monto fijo
  Dado un Ítem de 12 unidades con Sistema ISC 02 y monto fijo configurado de S/ 1.25 por unidad
  Cuando el sistema calcula
  Entonces ISC = 15.00

CA-11 (RF-05) — ICBPER fuera de la base del IGV
  Dado un Ítem "bolsa plástica" de 3 unidades con Valor unitario S/ 0.10 y ICBPER configurado de S/ 0.50 por bolsa
  Cuando el sistema calcula
  Entonces ICBPER = 1.50, IGV = 0.05 (18 % de 0.30, no de 1.80) e importe del Ítem = 1.85

CA-12 (RF-05) — Ítem exonerado
  Dado un Ítem con Afectación IGV 20
  Cuando el sistema calcula
  Entonces IGV = 0.00 y el Ítem suma en total exonerado

CA-13 (RF-06) — Totales del Comprobante
  Dados los Ítems de CA-08 y CA-12 (exonerado con valor de venta 20.00)
  Cuando el sistema calcula totales
  Entonces total gravado = 50.00, total exonerado = 20.00, total IGV = 9.00 e importe total = 79.00

CA-14 (RF-07) — Encabezado UBL
  Dada una Factura confirmada
  Cuando se genera el XML UBL
  Entonces contiene UBLVersionID = 2.1, CustomizationID = 2.0, InvoiceTypeCode = 01 con listID = 0101

CA-15 (RF-08) — XML inválido no se envía
  Dado un XML UBL sin el elemento obligatorio IssueDate
  Cuando se valida contra el XSD
  Entonces el proceso se detiene, el Estado SUNAT queda BORRADOR y no hay envío registrado

CA-16 (RF-09) — Firma presente
  Dado un XML UBL válido
  Cuando el sistema lo firma
  Entonces existe ds:Signature dentro de ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent y la firma verifica con el certificado del Emisor

CA-17 (RF-10) — CDR aceptado
  Dado un Comprobante FIRMADO
  Cuando el Validador SUNAT responde CDR con código 0
  Entonces el Estado SUNAT queda ACEPTADO y se guarda el CDR

CA-18 (RF-10) — CDR rechazado
  Dado un Comprobante FIRMADO con un error de negocio
  Cuando el Validador SUNAT responde con un código de rechazo
  Entonces el Estado SUNAT queda RECHAZADO y se muestra el código y la descripción devueltos

CA-19 (RF-11) — Sin conexión
  Dado que el Validador SUNAT no responde en 30 s
  Cuando el sistema intenta enviar
  Entonces el Estado SUNAT queda PENDIENTE_REENVIO y se programa un reintento

CA-20 (RF-11) — Alerta de plazo
  Dada una Factura en PENDIENTE_REENVIO emitida hace 2 días calendario
  Cuando corre la verificación de plazos
  Entonces se genera una alerta de vencimiento para el Administrador

CA-21 (RF-12) — Crédito sin cuotas
  Dada una Factura con Forma de pago Crédito y sin Cuotas
  Cuando el Vendedor intenta emitir
  Entonces el sistema bloquea con "Factura al crédito requiere cuotas"

CA-22 (RF-12) — Cuotas no cuadran
  Dada una Factura a crédito con Monto neto pendiente 1000.00 y Cuotas que suman 900.00
  Cuando el Vendedor intenta emitir
  Entonces el sistema bloquea con "La suma de cuotas difiere del monto pendiente"

CA-23 (RF-13) — Representación impresa
  Dado un Comprobante ACEPTADO
  Cuando el Vendedor pide descargar
  Entonces obtiene PDF con QR, XML firmado y CDR

CA-24 (RF-14) — Envío por correo
  Dado un Adquirente con correo registrado y un Comprobante ACEPTADO
  Cuando el sistema procesa la aceptación
  Entonces envía PDF y XML a ese correo

CA-25 (RF-15) — Leyenda en letras
  Dada una Factura de importe total 79.00 PEN
  Cuando se genera el XML UBL
  Entonces cbc:Note con languageLocaleID = 1000 contiene "SETENTA Y NUEVE CON 00/100 SOLES"

CA-26 (RF-16) — Contingencia fuera de plazo
  Dado un Comprobante de contingencia emitido hace 8 días calendario
  Cuando el Administrador intenta incluirlo en el informe
  Entonces el sistema advierte que el plazo de 7 días venció

CA-27 (RF-17) — Salida de inventario
  Dado un Comprobante ACEPTADO con 10 unidades del Producto P-001
  Cuando se registra la aceptación
  Entonces existe un Movimiento de inventario de salida de 10 unidades enlazado al Comprobante

## Fuera de alcance
- Guía de remisión, comprobantes de retención y percepción, liquidación de compra.
- Detracciones y operaciones de exportación (catálogo 51 distinto de 0101).
- Factoring y conformidad de factura por el Adquirente.
- Pagos en línea o pasarela de pagos.

## Clarificaciones
- [NEEDS CLARIFICATION] ¿El Emisor enviará directo a SUNAT (SEE-Del contribuyente) o a través de un OSE? ¿Cuál OSE?
- [NEEDS CLARIFICATION] ¿ABC tiene Certificado digital propio o usará el de un PSE?
- [NEEDS CLARIFICATION] ¿Qué Productos están afectos a ISC (licores, cerveza, cigarrillos, bebidas azucaradas) y con qué Sistema ISC y tasa o monto?
- [NEEDS CLARIFICATION] ¿Se entregan bolsas plásticas en el punto de venta (ICBPER)?
- [NEEDS CLARIFICATION] ¿Se vende en dólares? Si sí, ¿qué tipo de cambio se usa?
- [NEEDS CLARIFICATION] ¿Cuántos puntos de emisión hay (una Serie por caja o por local)?
- [NEEDS CLARIFICATION] ¿Las ventas al por mayor aplican descuentos por volumen? ¿Por Ítem o globales?
- [NEEDS CLARIFICATION] ¿Debe la app emitir sin internet y enviar después, o la Contingencia física basta?
