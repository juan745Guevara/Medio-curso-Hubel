# Glosario

Usa siempre estos términos, con este significado, en todos los archivos.

| Término | Definición |
|---|---|
| **Emisor** | ABC Software Corp como contribuyente que emite comprobantes; se identifica con su RUC. |
| **Adquirente** | Cliente que recibe el comprobante. Se identifica con un Documento de identidad del catálogo 06. |
| **RUC** | Registro Único de Contribuyentes: 11 dígitos, prefijo 10, 15, 16, 17 o 20, último dígito verificador por módulo 11. |
| **Documento de identidad** | Par tipo (catálogo 06) + número del Adquirente: 1 DNI, 4 Carné de extranjería, 6 RUC, 7 Pasaporte, 0 otros. |
| **Comprobante** | Comprobante de pago electrónico (CPE): Factura (01) o Boleta de venta (03). |
| **Nota** | Nota de crédito (07) o nota de débito (08) que modifica un Comprobante. |
| **Documento electrónico** | Comprobante o Nota. |
| **Serie** | Prefijo de 4 caracteres de un Documento electrónico: F### factura, B### boleta, FC##/BC## notas, etc. |
| **Correlativo** | Número secuencial de hasta 8 dígitos dentro de una Serie. |
| **Ítem** | Línea de un Documento electrónico: Producto, cantidad, Unidad de medida, valor unitario y tributos. |
| **Producto** | Bien que vende el Emisor, con código interno, descripción, Unidad de medida, Afectación IGV y, si aplica, Sistema ISC. |
| **Unidad de medida** | Código del catálogo 03 (UN/ECE Rec. 20), p. ej. NIU, KGM, LTR. |
| **Valor unitario** | Precio por unidad sin tributos. |
| **Precio unitario** | Precio por unidad con tributos incluidos (catálogo 16, código 01). |
| **Afectación IGV** | Código del catálogo 07 que dice si el Ítem es gravado, exonerado, inafecto o exportación. |
| **Sistema ISC** | Código del catálogo 08: 01 al valor, 02 monto fijo, 03 precio de venta al público. |
| **IGV** | Impuesto General a las Ventas. En el Comprobante se consigna la tasa total de 18 % (IGV + IPM). |
| **ISC** | Impuesto Selectivo al Consumo. Forma parte de la base del IGV. |
| **ICBPER** | Impuesto al consumo de bolsas de plástico, monto fijo por bolsa, fuera de la base del IGV. |
| **Tributo** | Código del catálogo 05 (1000 IGV, 2000 ISC, 7152 ICBPER, 9997 exonerado, 9998 inafecto, etc.). |
| **Tipo de operación** | Código del catálogo 51 (0101 venta interna). |
| **Forma de pago** | Contado o Crédito; si es Crédito, Monto neto pendiente y Cuotas. |
| **Cuota** | Monto y fecha de vencimiento de un pago a crédito. |
| **XML UBL** | Documento electrónico en formato XML conforme a UBL 2.1 de OASIS y al Anexo 9-A de SUNAT. |
| **XSD** | Esquema XML oficial de OASIS (`UBL-Invoice-2.1.xsd` y relacionados) contra el que se valida la estructura. |
| **Firma digital** | Firma XMLDSig enveloped del XML UBL con el Certificado digital del Emisor. |
| **Certificado digital** | Par de claves y certificado X.509 del Emisor usado para la Firma digital. |
| **Validador SUNAT** | SUNAT u OSE que recibe el XML UBL y devuelve un CDR. |
| **OSE** | Operador de Servicios Electrónicos autorizado por SUNAT para validar Documentos electrónicos. |
| **CDR** | Constancia de Recepción: respuesta del Validador SUNAT con estado aceptado, aceptado con observaciones o rechazado, y su código. |
| **Estado SUNAT** | Ciclo de vida de un Documento electrónico: BORRADOR, VALIDADO, FIRMADO, ENVIADO, ACEPTADO, OBSERVADO, RECHAZADO, PENDIENTE_REENVIO, DADO_DE_BAJA. |
| **Resumen diario** | Envío por lote que informa Boletas y sus Notas de un mismo día; devuelve un Ticket. |
| **Comunicación de baja** | Envío por lote que anula Documentos electrónicos ya informados. |
| **Ticket** | Identificador que devuelve el Validador SUNAT ante un envío por lote, para consultar su CDR. |
| **Contingencia** | Emisión excepcional en formato físico autorizado cuando no es posible emitir electrónicamente por causa no imputable al Emisor. |
| **Representación impresa** | PDF o ticket del Documento electrónico para el Adquirente, con código QR. |
| **Ficha legado** | Fila del software anterior: fecha, ingreso, salida, producto, nro_documento. |
| **Lote de migración** | Ejecución de migración identificada, con origen, fecha, conteos y hash. |
| **Movimiento de inventario** | Entrada o salida de Producto con fecha, cantidad y documento de soporte. |
| **Registro de ventas** | Registro formal de los Documentos electrónicos emitidos en un periodo (RS 234-2006). |
| **Periodo** | Mes tributario (AAAA-MM). |
