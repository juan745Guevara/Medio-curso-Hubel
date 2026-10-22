# Matriz de trazabilidad normativa: datos SUNAT ↔ UBL 2.1 ↔ DTO

Entregable 4 del examen. Cada fila cruza un dato que exige SUNAT con su catálogo, su cardinalidad en el XSD oficial de OASIS, su tag en el XML (mapeo del Anexo 9-A), el campo del DTO que lo alimenta, la regla, el código de error con que SUNAT u OSE rechaza si falla, y el CA que lo prueba.

**Lectura clave:** el XSD de OASIS solo obliga 6 elementos de la cabecera (`ID`, `IssueDate`, `AccountingSupplierParty`, `AccountingCustomerParty`, `LegalMonetaryTotal`, `InvoiceLine`). SUNAT vuelve obligatorios 22 datos más que en el XSD son opcionales (columna SUNAT = M con XSD 0..x). Por eso validar contra el XSD es necesario pero no suficiente: el sistema aplica además las reglas del Anexo 9-A y del catálogo de errores ([ADR 0003](../adr/0003-validar-xsd-antes-de-firmar.md)).

**Fuentes**: [UBL-Invoice-2.1.xsd](https://docs.oasis-open.org/ubl/os-UBL-2.1/xsd/maindoc/UBL-Invoice-2.1.xsd) y [UBL-CreditNote-2.1.xsd](https://docs.oasis-open.org/ubl/os-UBL-2.1/xsd/maindoc/UBL-CreditNote-2.1.xsd) de OASIS; tipos agregados consultados en [data2type UBL 2.1](https://repository.data2type.de/UBL/v_2.1/html/co.TaxSubtotalType.html); Anexo 1 y Anexo 9-A de la RS 097-2012 según [Anexo VII RS 114-2019](https://ww3.sunat.gob.pe/legislacion/superin/2019/anexoVII-114-2019.pdf); catálogos del Anexo 8 ([Anexo E RS 245-2017](https://www.sunat.gob.pe/legislacion/superin/2017/anexoE-245-2017.pdf), [Anexo D RS 357-2015](https://www.sunat.gob.pe/legislacion/superin/2015/anexoD-357-2015.pdf)); ICBPER [Anexos RS 150-2019](https://www.sunat.gob.pe/legislacion/superin/2019/anexos-150-2019.pdf); forma de pago [Anexo IV RS 193-2020](https://www.sunat.gob.pe/legislacion/superin/2020/anexo4-193-2020.pdf); códigos de error del catálogo de retorno de SUNAT en la copia mantenida por [Greenter (CodeErrors.xml)](https://github.com/thegreenter/xcodes/blob/master/src/data/CodeErrors.xml), a contrastar con el Excel de validaciones vigente del micrositio CPE.

Convenciones: `cbc` = CommonBasicComponents-2 · `cac` = CommonAggregateComponents-2 · `ext` = CommonExtensionComponents-2 · `ds` = xmldsig. **SUNAT**: M obligatorio, C condicional. **XSD**: cardinalidad del elemento (y de sus padres cuando importa); en **negrita** lo que el XSD ya obliga.

## A. Cabecera

| # | Dato SUNAT | Norma / anexo | Catálogo | SUNAT | XSD UBL 2.1 (min..max) | Tag UBL 2.1 | Campo DTO / origen | Regla / validación | Error SUNAT si falla | CA |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Firma digital | RS 097-2012 Anexo 9-A | — | M | `UBLExtensions` 0..1 | `ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/ds:Signature` | Generado por `FirmadorPort` | XMLDSig enveloped; certificado del Emisor | 1059, 2335, 2336 | CA-16 |
| 2 | Referencia de firma | Anexo 9-A | — | M | `Signature` 0..n · `ID` 1..1 | `cac:Signature/cbc:ID`, `cac:SignatoryParty`, `cac:DigitalSignatureAttachment` | Configuración del Emisor | Coincide con el Id de `ds:Signature` | 2076 | CA-16 |
| 3 | Versión UBL | Anexo 9-A | — | M | 0..1 | `cbc:UBLVersionID` | Constante `2.1` | Igual a "2.1" | 2074, 2075 | CA-14 |
| 4 | Versión de estructura | Anexo 9-A | — | M | 0..1 | `cbc:CustomizationID` | Constante `2.0` | Igual a "2.0" | 2072, 2073 | CA-14 |
| 5 | Serie y correlativo | Anexo 1 | — | M | **1..1** | `cbc:ID` | `serie` + correlativo asignado | `^[FB][A-Z0-9]{3}-[0-9]{1,8}$`; único; consecutivo; igual al nombre del archivo | 1001, 1002, 1035, 1049 | CA-01, CA-02 |
| 6 | Fecha de emisión | Anexo 1 | — | M | **1..1** | `cbc:IssueDate` | `fechaEmision` | yyyy-mm-dd; dentro del plazo | 1009, 1010, 2329 | CA-15 |
| 7 | Hora de emisión | Anexo 9-A | — | C | 0..1 | `cbc:IssueTime` | Reloj del servidor | hh:mm:ss | 2806 | — |
| 8 | Tipo de comprobante | Anexo 1 | 01 | M | 0..1 | `cbc:InvoiceTypeCode` | `tipo` | 01 factura / 03 boleta; igual al nombre del archivo | 1003, 1004 | CA-14 |
| 9 | Tipo de operación | Anexo 1 | 51 | M | atributo opcional | `cbc:InvoiceTypeCode/@listID` | `tipoOperacion` | 0101 en el alcance | 3205, 3206 | CA-14 |
| 10 | Leyenda importe en letras | Anexo 1 | 52 | M | 0..n | `cbc:Note[@languageLocaleID='1000']` | `ConversorMontoLetras(importeTotal)` | Coincide con importe total; código no repetido | 3006, 3014 | CA-25 |
| 11 | Moneda | Anexo 1 | 02 (ISO 4217) | M | 0..1 | `cbc:DocumentCurrencyCode` y `@currencyID` de todos los importes | `moneda` | PEN o USD; misma moneda en todo el documento | 2069, 2070, 3088, 2071 | — |
| 12 | Fecha de vencimiento | Anexo 1 | — | C | 0..1 | `cbc:DueDate` | Última cuota si Crédito | ≥ fecha de emisión | — | CA-21 |

## B. Emisor y Adquirente

| # | Dato SUNAT | Norma / anexo | Catálogo | SUNAT | XSD UBL 2.1 (min..max) | Tag UBL 2.1 | Campo DTO / origen | Regla / validación | Error SUNAT si falla | CA |
|---|---|---|---|---|---|---|---|---|---|---|
| 13 | RUC del Emisor | Anexo 1 | 06 (`6`) | M | `AccountingSupplierParty` **1..1** · `Party` 0..1 · `PartyIdentification` 0..n | `cac:AccountingSupplierParty/cac:Party/cac:PartyIdentification/cbc:ID/@schemeID=6` | Configuración del Emisor | Módulo 11; igual al RUC del nombre del archivo | 1008, 1034, 0154 | CA-03 |
| 14 | Razón social del Emisor | Anexo 1 | — | M | `PartyLegalEntity` 0..n · `RegistrationName` 0..1 | `…/cac:Party/cac:PartyLegalEntity/cbc:RegistrationName` | Configuración | No vacío | 1037 | — |
| 15 | Código de establecimiento | Anexo 1 | — | M | `RegistrationAddress` 0..1 · `AddressTypeCode` 0..1 | `…/cac:PartyLegalEntity/cac:RegistrationAddress/cbc:AddressTypeCode` | Configuración (0000 = domicilio fiscal) | 4 dígitos; declarado en el RUC | 3030, 3268 | — |
| 16 | Domicilio fiscal y ubigeo | Anexo 1 | 13 (INEI) | C | `ID` 0..1 · `AddressLine` 0..n | `…/cac:RegistrationAddress/cbc:ID`, `cac:AddressLine/cbc:Line` | Configuración | Ubigeo de 6 dígitos | — | — |
| 17 | Tipo de doc. del Adquirente | Anexo 1 | 06 | M (factura) / C (boleta) | `AccountingCustomerParty` **1..1** · `PartyIdentification` 0..n | `cac:AccountingCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID/@schemeID` | `adquirente.documento.tipo` | Factura ⇒ 6 | 2015, 2016, 2800, 2017 | CA-05 |
| 18 | Número de doc. del Adquirente | Anexo 1 | — | M / C | `PartyIdentification/ID` **1..1** | `…/cac:PartyIdentification/cbc:ID` | `adquirente.documento.numero` | RUC módulo 11; DNI 8 dígitos | 2014, 2801, 2802 | CA-03, CA-04 |
| 19 | Nombre del Adquirente | Anexo 1 | — | M / C | `RegistrationName` 0..1 | `…/cac:PartyLegalEntity/cbc:RegistrationName` | `adquirente.nombre` | Boleta > S/ 700 ⇒ obligatorio | 2021, 2022 | CA-06, CA-07 |

## C. Forma de pago (RS 193-2020)

| # | Dato SUNAT | Norma / anexo | Catálogo | SUNAT | XSD UBL 2.1 (min..max) | Tag UBL 2.1 | Campo DTO / origen | Regla / validación | Error SUNAT si falla | CA |
|---|---|---|---|---|---|---|---|---|---|---|
| 20 | Forma de pago contado | Anexo IV RS 193-2020 | — | M (factura) | `PaymentTerms` 0..n · `ID` 0..1 · `PaymentMeansID` 0..n | `cac:PaymentTerms[cbc:ID='FormaPago']/cbc:PaymentMeansID='Contado'` | `formaPago.tipo` | Contado o Credito, no ambos | 3244, 3245, 3247 | — |
| 21 | Crédito y monto pendiente | Anexo IV RS 193-2020 | 02 | C | `Amount` 0..1 | `cac:PaymentTerms[cbc:ID='FormaPago']/cbc:PaymentMeansID='Credito'` + `cbc:Amount` | `formaPago.montoPendiente` | Obligatorio si Crédito; ≤ importe total | 3250, 3251, 3265 | CA-21 |
| 22 | Cuotas | Anexo IV RS 193-2020 | — | C | `PaymentDueDate` 0..1 | `cac:PaymentTerms/cbc:PaymentMeansID='Cuota001'…`, `cbc:Amount`, `cbc:PaymentDueDate` | `formaPago.cuotas[]` | ≥ 1; Σ = monto pendiente; fecha ≥ emisión | 3246, 3249, 3252, 3253, 3266, 3267 | CA-21, CA-22 |

## D. Ítem (`/Invoice/cac:InvoiceLine/…`)

| # | Dato SUNAT | Norma / anexo | Catálogo | SUNAT | XSD UBL 2.1 (min..max) | Tag UBL 2.1 | Campo DTO / origen | Regla / validación | Error SUNAT si falla | CA |
|---|---|---|---|---|---|---|---|---|---|---|
| 23 | Número de orden | Anexo 1 | — | M | `InvoiceLine` **1..n** · `ID` **1..1** | `cbc:ID` | orden del ítem | 1..n, sin repetir | 2023, 2752 | — |
| 24 | Cantidad y unidad | Anexo 1 | 03 (UN/ECE Rec 20) | M | 0..1 | `cbc:InvoicedQuantity` + `@unitCode` | `items[].cantidad`, `items[].unidad` | Unidad en catálogo; cantidad > 0 | 2024, 2025, 2883, 4320 | CA-08 |
| 25 | Valor de venta del ítem | Anexo 1 | — | M | **1..1** | `cbc:LineExtensionAmount` | Calculado: cantidad × valorUnitario − descuento | 2 decimales; cuadra con cantidad × valor | 2032, 2370, 4288 | CA-08 |
| 26 | Precio unitario (con tributos) | Anexo 1 | 16 (`01`) | M | `PricingReference` 0..1 · `AlternativeConditionPrice` 0..n · `PriceTypeCode` 0..1 | `cac:PricingReference/cac:AlternativeConditionPrice/cbc:PriceAmount` + `cbc:PriceTypeCode=01` | Calculado | (valor venta + tributos) / cantidad | 2028, 2029, 2030, 2367, 2409, 2410, 4287 | CA-08 |
| 27 | Valor unitario (sin tributos) | Anexo 1 | — | M | `Price` 0..1 · `PriceAmount` 1..1 | `cac:Price/cbc:PriceAmount` | `items[].valorUnitario` | ≥ 0 | 2067, 2068 | CA-08 |
| 28 | Descripción | Anexo 1 | — | M | `Item` **1..1** · `Description` 0..n | `cac:Item/cbc:Description` | `items[].descripcion` o Producto | ≤ 500 | 2026, 2027 | — |
| 29 | Código del producto | Anexo 1 | — | C | `SellersItemIdentification` 0..1 | `cac:Item/cac:SellersItemIdentification/cbc:ID` | `items[].codigoProducto` | Producto clasificado | 4269 | CA-47 |
| 30 | Código de producto SUNAT | Anexo 1 | 25 (UNSPSC) | C | `CommodityClassification` 0..n | `cac:Item/cac:CommodityClassification/cbc:ItemClassificationCode` | Producto | Opcional en el alcance; mínimo tercer nivel UNSPSC | 4331, 4337 | — |
| 31 | Afectación al IGV | Anexo 1 | **07** | M | 0..1 | `cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cbc:TaxExemptionReasonCode` | Producto.afectacion | 10 gravado; 20 exonerado; 30 inafecto | 2040, 2371 | CA-08, CA-12 |
| 32 | IGV del ítem | Anexo 1 | 05 (`1000`, VAT) | M | `TaxTotal` 0..n · `TaxAmount` 1..1 · `TaxSubtotal` 0..n · `TaxCategory` 1..1 · `Percent` 0..1 · `TaxScheme` 1..1 | `cac:TaxTotal/cac:TaxSubtotal` (`cbc:TaxableAmount`, `cbc:TaxAmount`, `cac:TaxCategory/cbc:Percent=18`, `cac:TaxScheme/cbc:ID=1000`) | Calculado | IGV = 18 % × (valor venta + ISC); un solo tributo de afectación por línea | 2033, 2034, 2035, 2036, 2037, 3103, 3105, 3106, 3111, 4290 | CA-08, CA-09 |
| 33 | **Sistema de ISC del ítem** | Anexo 1 | **08** | C | `TierRange` 0..1 | `cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cbc:TierRange` | Producto.isc.sistema | 01 al valor / 02 monto fijo / 03 precio de venta al público; solo si el tributo es ISC | 2041, 2373, 3210 | CA-09, CA-10 |
| 34 | Monto del ISC del ítem | Anexo 1 | 05 (`2000`, EXC) | C | `TaxSubtotal` 0..n | `cac:TaxSubtotal` con `cac:TaxScheme/cbc:ID=2000`, `cbc:TaxableAmount`, `cbc:TaxAmount`, `cbc:Percent` | Calculado | Sistema 01: tasa × valor venta; 02: monto × cantidad; 03: según Apéndice IV lit. C | 3104, 3108 | CA-09, CA-10 |
| 35 | ICBPER del ítem | [Anexos RS 150-2019](https://www.sunat.gob.pe/legislacion/superin/2019/anexos-150-2019.pdf) | 05 (`7152`, OTH) | C | `BaseUnitMeasure` 0..1 · `PerUnitAmount` 0..1 | `cac:TaxSubtotal/cbc:BaseUnitMeasure` (bolsas) y `cac:TaxSubtotal/cac:TaxCategory/cbc:PerUnitAmount` (tasa), `cac:TaxScheme/cbc:ID=7152` | Producto.esBolsaPlastica, cantidad | S/ 0.50 × bolsas; fuera de base IGV | 2949, 3236, 3237, 3238 | CA-11 |
| 36 | Exonerado / inafecto del ítem | Anexo 1 | 05 (`9997` EXO, `9998` INA) | C | `TaxSubtotal` 0..n | `cac:TaxSubtotal` con `cac:TaxScheme/cbc:ID=9997` o `9998`, `cbc:TaxAmount=0` | Calculado | TaxAmount 0 | 3101, 3110 | CA-12 |

## E. Totales

| # | Dato SUNAT | Norma / anexo | Catálogo | SUNAT | XSD UBL 2.1 (min..max) | Tag UBL 2.1 | Campo DTO / origen | Regla / validación | Error SUNAT si falla | CA |
|---|---|---|---|---|---|---|---|---|---|---|
| 37 | Total IGV | Anexo 1 | 05 | M | `TaxTotal` 0..n · `TaxAmount` **1..1** | `/Invoice/cac:TaxTotal/cbc:TaxAmount` y `cac:TaxSubtotal[TaxScheme/ID=1000]` | `totales.igv` | Σ IGV de ítems = total de tributos | 2048, 2049, 2638, 4301 | CA-13 |
| 38 | Total valor de venta gravado | Anexo 1 | 05 | C | `TaxableAmount` 0..1 | `cac:TaxSubtotal[ID=1000]/cbc:TaxableAmount` | `totales.gravado` | Σ ítems 10–17 | 3010, 3011, 4294 | CA-13 |
| 39 | Total exonerado / inafecto | Anexo 1 | 05 | C | `TaxableAmount` 0..1 | `cac:TaxSubtotal[ID=9997 / 9998]/cbc:TaxableAmount` | `totales.exonerado`, `totales.inafecto` | Σ ítems 20–21 / 30–37 | 4296, 4297 | CA-13 |
| 40 | Total ISC | Anexo 1 | 05 | C | `TaxAmount` 1..1 | `cac:TaxSubtotal[ID=2000]/cbc:TaxAmount` | `totales.isc` | Σ ISC | 2638, 4301 | CA-09 |
| 41 | Total ICBPER | RS 150-2019 | 05 | C | `TaxAmount` 1..1 | `cac:TaxSubtotal[ID=7152]/cbc:TaxAmount` | `totales.icbper` | Σ ICBPER | 4321 | CA-11 |
| 42 | Total valor de venta | Anexo 1 | — | M | `LegalMonetaryTotal` **1..1** · `LineExtensionAmount` 0..1 | `cac:LegalMonetaryTotal/cbc:LineExtensionAmount` | Σ valor venta | 2 decimales | 2487, 2488, 4309 | CA-13 |
| 43 | Total precio de venta | Anexo 1 | — | M | `TaxInclusiveAmount` 0..1 | `cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount` | Σ valor venta + tributos | Igual al calculado | 2592, 3019, 4211 | CA-13 |
| 44 | Descuentos globales | Anexo 1 | 53 | C | `AllowanceTotalAmount` 0..1 | `cac:AllowanceCharge` + `cac:LegalMonetaryTotal/cbc:AllowanceTotalAmount` | `totales.descuentos` | Pendiente de clarificación | 2065, 2419 | — |
| 45 | Importe total | Anexo 1 | — | M | `PayableAmount` **1..1** | `cac:LegalMonetaryTotal/cbc:PayableAmount` | `totales.importeTotal` | = 43 − 44; redondeo dentro de lo permitido | 2043, 2044, 4314 | CA-13 |

## F. Notas (CreditNote / DebitNote)

En `UBL-CreditNote-2.1.xsd`: `ID`, `IssueDate`, `AccountingSupplierParty`, `AccountingCustomerParty`, `LegalMonetaryTotal` son 1..1 y `CreditNoteLine` 1..n; `DiscrepancyResponse` y `BillingReference` son 0..n aunque SUNAT los exige.

| # | Dato SUNAT | Norma / anexo | Catálogo | SUNAT | XSD UBL 2.1 (min..max) | Tag UBL 2.1 | Campo DTO / origen | Regla / validación | Error SUNAT si falla | CA |
|---|---|---|---|---|---|---|---|---|---|---|
| 46 | Tipo de nota de crédito | Anexo 3 | **09** | M | `DiscrepancyResponse` 0..n · `ResponseCode` 0..1 | `/CreditNote/cac:DiscrepancyResponse/cbc:ResponseCode` | `motivo.codigo` | 01–10, 13 | 2127, 2128 | CA-28 |
| 47 | Tipo de nota de débito | Anexo 4 | **10** | M | `DiscrepancyResponse` 0..n · `ResponseCode` 0..1 | `/DebitNote/cac:DiscrepancyResponse/cbc:ResponseCode` | `motivo.codigo` | 01–03 | 2172, 2173 | CA-28 |
| 48 | Sustento | Anexo 3/4 | — | M | `Description` 0..n | `cac:DiscrepancyResponse/cbc:Description` | `motivo.descripcion` | No vacío | 2135, 2136 | CA-28 |
| 49 | Documento que modifica | Anexo 3/4 | 01 | M | `BillingReference` 0..n · `InvoiceDocumentReference` 0..1 · `ID` 1..1 · `DocumentTypeCode` 0..1 | `cac:BillingReference/cac:InvoiceDocumentReference/cbc:ID` y `cbc:DocumentTypeCode` | `documentoModificado` | Uno solo; existe; no dado de baja ni rechazado | 2116, 2117, 2119, 2120, 2121, 2412 | CA-30, CA-32 |

## G. Del error SUNAT al error de la API

Cada regla se valida **antes** de enviar, con el código interno de la API (`error.schema.json`, campo `code`). Si aun así SUNAT/OSE rechaza, su código se devuelve en `sunatCodigo` (RF-10, CA-18).

| Código interno (API) | Validación local | Errores SUNAT que evita | CA |
|---|---|---|---|
| `RUC_INVALIDO` | Módulo 11 | 2802, 1034 | CA-04 |
| `FACTURA_REQUIERE_RUC` | Factura ⇒ tipo 6 | 2016, 2017, 2800 | CA-05 |
| `BOLETA_REQUIERE_DOCUMENTO` | Boleta > S/ 700 ⇒ documento y nombre | 2014, 2015, 2021 | CA-06 |
| `CREDITO_SIN_CUOTAS` | Crédito ⇒ ≥ 1 cuota y monto pendiente | 3249, 3251 | CA-21 |
| `CUOTAS_NO_CUADRAN` | Σ cuotas = pendiente ≤ total | 3265, 3266 | CA-22 |
| `XSD_INVALIDO` | XSD OASIS 2.1 | 1002, 1010, 2075, 2073, 2063 | CA-15 |
| `TOTALES_INCONSISTENTES` | Recalcular líneas y totales | 4287, 4288, 4290, 4301, 4309, 4211, 4321 | CA-08 a CA-13 |
| `SISTEMA_ISC_INVALIDO` | Catálogo 08 solo con tributo 2000 | 2041, 2373, 3210 | CA-09, CA-10 |
| `NOTA_MULTIPLE` | Una nota, un comprobante | 2412 | CA-32 |
| `DOC_NO_ACEPTADO` | Documento modificado ACEPTADO/OBSERVADO | 2119, 2120, 2121 | CA-30 |
| `BAJA_FUERA_DE_PLAZO` | 7 días calendario | 2374 | CA-35 |
| `ENVIO_FUERA_DE_PLAZO` (alerta) | Plazo de envío | 2108, 2600 | CA-20 |
| `FIRMA_INVALIDA` | Verificar firma tras firmar; no reformatear | 1059, 2335, 2336 | CA-16 |

## H. Cruce con la feature y el puerto

| Bloque | Feature | Puerto de salida que genera el tag |
|---|---|---|
| A, B, C, D, E | 001 | `GeneradorUblPort` (datos), `FirmadorPort` (filas 1–2) |
| F | 002 | `GeneradorUblPort` |
| Envío del archivo `RUC-TT-SERIE-NUM.zip` | 001, 002 | `ValidadorSunatPort` |

## Lagunas de la matriz
- Fórmula del sistema ISC 03 (factor sobre precio de venta al público): confirmar con el Apéndice IV lit. C y la guía SUNAT.
- Precisión decimal máxima de valores unitarios y tolerancia de redondeo de totales (error 4314): confirmar en el Excel de validaciones vigente.
- Los códigos de error son los del grupo Factura; la boleta y las notas tienen equivalentes con otra numeración (p. ej. 2110/2111 en lugar de 2074/2075).
