# Matriz de trazabilidad normativa: datos SUNAT ↔ UBL 2.1 ↔ DTO

Entregable 4 del examen. Cada fila cruza un dato que exige SUNAT con su catálogo, su tag en el XML UBL 2.1 (mapeo del Anexo 9-A), el campo del DTO que lo alimenta y la regla o CA que lo prueba.

**Fuentes**: Anexo 1 (factura) y Anexo 9-A de la RS 097-2012 según [Anexo VII RS 114-2019](https://ww3.sunat.gob.pe/legislacion/superin/2019/anexoVII-114-2019.pdf); catálogos del Anexo 8 ([Anexo E RS 245-2017](https://www.sunat.gob.pe/legislacion/superin/2017/anexoE-245-2017.pdf), [Anexo D RS 357-2015](https://www.sunat.gob.pe/legislacion/superin/2015/anexoD-357-2015.pdf)); forma de pago [Anexo IV RS 193-2020](https://www.sunat.gob.pe/legislacion/superin/2020/anexo4-193-2020.pdf); esquema [OASIS UBL 2.1](https://docs.oasis-open.org/ubl/UBL-2.1.html).

Prefijos: `cbc` = CommonBasicComponents-2 · `cac` = CommonAggregateComponents-2 · `ext` = CommonExtensionComponents-2 · `ds` = xmldsig. Obligatoriedad: M = obligatorio, C = condicional (según SUNAT).

## A. Cabecera

| # | Dato SUNAT | Norma / anexo | Catálogo | Oblig. | Tag UBL 2.1 (`/Invoice/…`) | Campo DTO / origen | Regla / validación | CA |
|---|---|---|---|---|---|---|---|---|
| 1 | Firma digital | RS 097-2012 Anexo 9-A | — | M | `ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/ds:Signature` | Generado por `FirmadorPort` | XMLDSig enveloped; certificado del Emisor | CA-16 |
| 2 | Referencia de firma | Anexo 9-A | — | M | `cac:Signature/cbc:ID`, `cac:SignatoryParty`, `cac:DigitalSignatureAttachment` | Configuración del Emisor | Coincide con el Id de `ds:Signature` | CA-16 |
| 3 | Versión UBL | Anexo 9-A | — | M | `cbc:UBLVersionID` | Constante `2.1` | Igual a "2.1" | CA-14 |
| 4 | Versión de estructura | Anexo 9-A | — | M | `cbc:CustomizationID` | Constante `2.0` | Igual a "2.0" | CA-14 |
| 5 | Serie y correlativo | Anexo 1 | — | M | `cbc:ID` | `serie` + correlativo asignado | `^[FB][A-Z0-9]{3}-[0-9]{1,8}$`; único; consecutivo | CA-01, CA-02 |
| 6 | Fecha de emisión | Anexo 1 | — | M | `cbc:IssueDate` | `fechaEmision` | yyyy-mm-dd; no futura | CA-15 |
| 7 | Hora de emisión | Anexo 9-A | — | C | `cbc:IssueTime` | Reloj del servidor | hh:mm:ss | — |
| 8 | Tipo de comprobante | Anexo 1 | 01 | M | `cbc:InvoiceTypeCode` | `tipo` | 01 factura / 03 boleta | CA-14 |
| 9 | Tipo de operación | Anexo 1 | 51 | M | `cbc:InvoiceTypeCode/@listID` | `tipoOperacion` | 0101 en el alcance | CA-14 |
| 10 | Leyenda importe en letras | Anexo 1 | 52 | M | `cbc:Note[@languageLocaleID='1000']` | `ConversorMontoLetras(importeTotal)` | Coincide con importe total | CA-25 |
| 11 | Moneda | Anexo 1 | 02 (ISO 4217) | M | `cbc:DocumentCurrencyCode` y `@currencyID` de todos los importes | `moneda` | PEN o USD; mismo valor en todos los importes | — |
| 12 | Fecha de vencimiento | Anexo 1 | — | C | `cbc:DueDate` | Última cuota si Crédito | ≥ fecha de emisión | CA-21 |

## B. Emisor y Adquirente

| # | Dato SUNAT | Norma / anexo | Catálogo | Oblig. | Tag UBL 2.1 | Campo DTO / origen | Regla / validación | CA |
|---|---|---|---|---|---|---|---|---|
| 13 | RUC del Emisor | Anexo 1 | 06 (`6`) | M | `cac:AccountingSupplierParty/cac:Party/cac:PartyIdentification/cbc:ID/@schemeID=6` | Configuración del Emisor | Módulo 11 | CA-03 |
| 14 | Razón social del Emisor | Anexo 1 | — | M | `…/cac:Party/cac:PartyLegalEntity/cbc:RegistrationName` | Configuración | No vacío | — |
| 15 | Código de establecimiento | Anexo 1 | — | M | `…/cac:PartyLegalEntity/cac:RegistrationAddress/cbc:AddressTypeCode` | Configuración (0000 = domicilio fiscal) | 4 dígitos | — |
| 16 | Domicilio fiscal y ubigeo | Anexo 1 | 13 (INEI) | C | `…/cac:RegistrationAddress/cbc:ID`, `cac:AddressLine/cbc:Line` | Configuración | Ubigeo de 6 dígitos | — |
| 17 | Tipo de doc. del Adquirente | Anexo 1 | 06 | M (factura) / C (boleta) | `cac:AccountingCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID/@schemeID` | `adquirente.documento.tipo` | Factura ⇒ 6 | CA-05 |
| 18 | Número de doc. del Adquirente | Anexo 1 | — | M / C | `…/cac:PartyIdentification/cbc:ID` | `adquirente.documento.numero` | RUC módulo 11; DNI 8 dígitos | CA-03, CA-04 |
| 19 | Nombre del Adquirente | Anexo 1 | — | M / C | `…/cac:PartyLegalEntity/cbc:RegistrationName` | `adquirente.nombre` | Boleta > S/ 700 ⇒ obligatorio | CA-06, CA-07 |

## C. Forma de pago (RS 193-2020)

| # | Dato SUNAT | Norma | Catálogo | Oblig. | Tag UBL 2.1 | Campo DTO | Regla | CA |
|---|---|---|---|---|---|---|---|---|
| 20 | Forma de pago contado | Anexo IV RS 193-2020 | — | M (factura) | `cac:PaymentTerms[cbc:ID='FormaPago']/cbc:PaymentMeansID='Contado'` | `formaPago.tipo` | Contado o Credito | — |
| 21 | Crédito y monto pendiente | Anexo IV RS 193-2020 | 02 | C | `cac:PaymentTerms[cbc:ID='FormaPago']/cbc:PaymentMeansID='Credito'` + `cbc:Amount` | `formaPago.montoPendiente` | Obligatorio si Crédito | CA-21 |
| 22 | Cuotas | Anexo IV RS 193-2020 | — | C | `cac:PaymentTerms/cbc:PaymentMeansID='Cuota001'…`, `cbc:Amount`, `cbc:PaymentDueDate` | `formaPago.cuotas[]` | ≥ 1; Σ = monto pendiente | CA-21, CA-22 |

## D. Ítem

| # | Dato SUNAT | Norma | Catálogo | Oblig. | Tag UBL 2.1 (`/Invoice/cac:InvoiceLine/…`) | Campo DTO | Regla | CA |
|---|---|---|---|---|---|---|---|---|
| 23 | Número de orden | Anexo 1 | — | M | `cbc:ID` | orden del ítem | 1..n | — |
| 24 | Cantidad y unidad | Anexo 1 | 03 (UN/ECE Rec 20) | M | `cbc:InvoicedQuantity` + `@unitCode` | `items[].cantidad`, `items[].unidad` | Unidad en catálogo; cantidad > 0 | CA-08 |
| 25 | Valor de venta del ítem | Anexo 1 | — | M | `cbc:LineExtensionAmount` | Calculado: cantidad × valorUnitario − descuento | 2 decimales | CA-08 |
| 26 | Precio unitario (con tributos) | Anexo 1 | 16 (`01`) | M | `cac:PricingReference/cac:AlternativeConditionPrice/cbc:PriceAmount` + `cbc:PriceTypeCode=01` | Calculado | (valor venta + tributos) / cantidad | CA-08 |
| 27 | Valor unitario (sin tributos) | Anexo 1 | — | M | `cac:Price/cbc:PriceAmount` | `items[].valorUnitario` | ≥ 0 | CA-08 |
| 28 | Descripción | Anexo 1 | — | M | `cac:Item/cbc:Description` | `items[].descripcion` o Producto | ≤ 500 | — |
| 29 | Código del producto | Anexo 1 | — | C | `cac:Item/cac:SellersItemIdentification/cbc:ID` | `items[].codigoProducto` | Producto clasificado | CA-47 |
| 30 | Código de producto SUNAT | Anexo 1 | 25 (UNSPSC) | C | `cac:Item/cac:CommodityClassification/cbc:ItemClassificationCode` | Producto | Opcional en el alcance | — |
| 31 | Afectación al IGV | Anexo 1 | **07** | M | `cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cbc:TaxExemptionReasonCode` | Producto.afectacion | 10 gravado; 20 exonerado; 30 inafecto | CA-08, CA-12 |
| 32 | IGV del ítem | Anexo 1 | 05 (`1000`, VAT) | M | `cac:TaxTotal/cac:TaxSubtotal` (`cbc:TaxableAmount`, `cbc:TaxAmount`, `cac:TaxCategory/cbc:Percent=18`, `cac:TaxScheme/cbc:ID=1000`) | Calculado | IGV = 18 % × (valor venta + ISC) | CA-08, CA-09 |
| 33 | **Sistema de ISC del ítem** | Anexo 1 | **08** | C | `cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cbc:TierRange` | Producto.isc.sistema | 01 al valor / 02 monto fijo / 03 precio de venta al público | CA-09, CA-10 |
| 34 | Monto del ISC del ítem | Anexo 1 | 05 (`2000`, EXC) | C | `cac:TaxSubtotal` con `cac:TaxScheme/cbc:ID=2000`, `cbc:TaxableAmount`, `cbc:TaxAmount`, `cbc:Percent` | Calculado | Sistema 01: tasa × valor venta; 02: monto × cantidad; 03: según Apéndice IV lit. C | CA-09, CA-10 |
| 35 | ICBPER del ítem | RS 150-2019 | 05 (`7152`, por verificar) | C | `cac:TaxSubtotal` con `cac:TaxScheme/cbc:ID=7152`, `cbc:BaseUnitMeasure` (bolsas), `cbc:PerUnitAmount` | Producto.esBolsaPlastica, cantidad | S/ 0.50 × bolsas; fuera de base IGV | CA-11 |
| 36 | Exonerado / inafecto del ítem | Anexo 1 | 05 (`9997` EXO, `9998` INA) | C | `cac:TaxSubtotal` con `cac:TaxScheme/cbc:ID=9997` o `9998`, `cbc:TaxAmount=0` | Calculado | TaxAmount 0 | CA-12 |

## E. Totales

| # | Dato SUNAT | Norma | Catálogo | Oblig. | Tag UBL 2.1 | Campo DTO | Regla | CA |
|---|---|---|---|---|---|---|---|---|
| 37 | Total IGV | Anexo 1 | 05 | M | `/Invoice/cac:TaxTotal/cbc:TaxAmount` y `cac:TaxSubtotal[TaxScheme/ID=1000]` | `totales.igv` | Σ IGV de ítems | CA-13 |
| 38 | Total valor de venta gravado | Anexo 1 | 05 | C | `cac:TaxSubtotal[ID=1000]/cbc:TaxableAmount` | `totales.gravado` | Σ ítems 10–17 | CA-13 |
| 39 | Total exonerado / inafecto | Anexo 1 | 05 | C | `cac:TaxSubtotal[ID=9997 / 9998]/cbc:TaxableAmount` | `totales.exonerado`, `totales.inafecto` | Σ ítems 20–21 / 30–37 | CA-13 |
| 40 | Total ISC | Anexo 1 | 05 | C | `cac:TaxSubtotal[ID=2000]/cbc:TaxAmount` | `totales.isc` | Σ ISC | CA-09 |
| 41 | Total ICBPER | RS 150-2019 | 05 | C | `cac:TaxSubtotal[ID=7152]/cbc:TaxAmount` | `totales.icbper` | Σ ICBPER | CA-11 |
| 42 | Total valor de venta | Anexo 1 | — | M | `cac:LegalMonetaryTotal/cbc:LineExtensionAmount` | Σ valor venta | 2 decimales | CA-13 |
| 43 | Total precio de venta | Anexo 1 | — | M | `cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount` | Σ valor venta + tributos | — | CA-13 |
| 44 | Descuentos globales | Anexo 1 | 53 | C | `cac:AllowanceCharge` + `cac:LegalMonetaryTotal/cbc:AllowanceTotalAmount` | `totales.descuentos` | Pendiente de clarificación | — |
| 45 | Importe total | Anexo 1 | — | M | `cac:LegalMonetaryTotal/cbc:PayableAmount` | `totales.importeTotal` | = 43 − 44 | CA-13 |

## F. Notas (CreditNote / DebitNote)

| # | Dato SUNAT | Norma | Catálogo | Oblig. | Tag UBL 2.1 | Campo DTO | Regla | CA |
|---|---|---|---|---|---|---|---|---|
| 46 | Tipo de nota de crédito | Anexo 3 | **09** | M | `/CreditNote/cac:DiscrepancyResponse/cbc:ResponseCode` | `motivo.codigo` | 01–10, 13 | CA-28 |
| 47 | Tipo de nota de débito | Anexo 4 | **10** | M | `/DebitNote/cac:DiscrepancyResponse/cbc:ResponseCode` | `motivo.codigo` | 01–03 | CA-28 |
| 48 | Sustento | Anexo 3/4 | — | M | `cac:DiscrepancyResponse/cbc:Description` | `motivo.descripcion` | No vacío | CA-28 |
| 49 | Documento que modifica | Anexo 3/4 | 01 | M | `cac:BillingReference/cac:InvoiceDocumentReference/cbc:ID` y `cbc:DocumentTypeCode` | `documentoModificado` | Uno solo; ACEPTADO/OBSERVADO | CA-30, CA-32 |

## G. Cruce con la feature y el puerto

| Bloque | Feature | Puerto de salida que genera el tag |
|---|---|---|
| A, B, C, D, E | 001 | `GeneradorUblPort` (datos), `FirmadorPort` (filas 1–2) |
| F | 002 | `GeneradorUblPort` |
| Envío del archivo `RUC-TT-SERIE-NUM.zip` | 001, 002 | `ValidadorSunatPort` |

## Lagunas de la matriz
- Tag exacto del ICBPER (`BaseUnitMeasure`/`PerUnitAmount`) y su código 7152: confirmar con la guía XML UBL 2.1 de factura de SUNAT antes de implementar.
- Precisión decimal máxima de valores unitarios: confirmar en el Excel de validaciones.
- Fórmula del sistema ISC 03 (factor sobre precio de venta al público): confirmar con el Apéndice IV y la guía SUNAT.
