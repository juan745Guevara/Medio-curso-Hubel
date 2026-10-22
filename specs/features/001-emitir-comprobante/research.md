# Research 001 — Normativa y estándar para emitir

Verificado el 22/10/2026. "Por verificar" = no se abrió la fuente primaria.

| Tema | Qué exige | Fuente | RF |
|---|---|---|---|
| Sistema de emisión | RS 097-2012 crea el SEE-Del contribuyente; RS 117-2017 el SEE-OSE. El Emisor genera, firma y envía. | [RS 114-2019 (consolida)](https://ww3.sunat.gob.pe/legislacion/superin/2019/114-2019.pdf) | RF-07, RF-10 |
| UBL 2.1 obligatorio | Desde el 1/3/2019 solo UBL 2.1 para factura, boleta y notas. | [SNI](https://sni.org.pe/modifican-la-fecha-usar-obligatoriamente-la-version-2-1-del-formato-xml-estandar-ubl-sistema-emision-electronica-operador-servicios-electronicos-sistema-emision-ele/) | RF-07 |
| Esquema OASIS | `UBL-Invoice-2.1.xsd`, namespace `urn:oasis:names:specification:ubl:schema:xsd:Invoice-2`, importa cac, cbc, ext. | [OASIS UBL 2.1](https://docs.oasis-open.org/ubl/UBL-2.1.html) | RF-08 |
| Mapeo SUNAT | Anexo 9-A: UBLVersionID "2.1", CustomizationID "2.0", InvoiceTypeCode con catálogo 01, moneda catálogo 02. | [Anexo VII RS 114-2019](https://ww3.sunat.gob.pe/legislacion/superin/2019/anexoVII-114-2019.pdf) | RF-07 |
| Catálogos | Anexo 8: 01 tipo doc, 02 moneda, 03 unidad, 05 tributos, 06 doc. identidad, 07 afectación IGV, 08 sistema ISC, 09/10 notas, 16 tipo precio, 51 tipo operación, 52 leyendas. | [Anexo E RS 245-2017](https://www.sunat.gob.pe/legislacion/superin/2017/anexoE-245-2017.pdf), [Anexo D RS 357-2015](https://www.sunat.gob.pe/legislacion/superin/2015/anexoD-357-2015.pdf) | RF-05, RF-07 |
| Catálogo 08 | Campo `cbc:TierRange`: 01 Sistema al valor (Apéndice IV lit. A), 02 Monto fijo (lit. B), 03 Precios de venta al público (lit. C). | [Anexo D RS 357-2015](https://www.sunat.gob.pe/legislacion/superin/2015/anexoD-357-2015.pdf) | RF-05 |
| Cálculo ISC | Tres sistemas; en "al valor" la base es el valor de venta y se aplica la tasa del Apéndice IV. | [SUNAT orientación](https://orientacion.sunat.gob.pe/node/1809) | RF-05 |
| Tasa IGV | 18 % total. Desde 2026: 15.5 % IGV + 2.5 % IPM (Ley 32387); en el Comprobante se consigna 18. | [SUNAT orientación](https://orientacion.sunat.gob.pe/node/1737) | RF-05 |
| ICBPER | S/ 0.50 por bolsa desde 2023; no forma parte de la base del IGV; consignar sigla ICBPER e importe. | [LP Derecho](https://lpderecho.pe/impuesto-consumo-bolsas-plastico/), [RS 150-2019](https://ww3.sunat.gob.pe/legislacion/superin/2019/150-2019.pdf) | RF-05 |
| Código tributo ICBPER | 7152 en catálogo 05 (código internacional OTH). Bolsas en `cac:TaxSubtotal/cbc:BaseUnitMeasure`; tasa en `cac:TaxSubtotal/cac:TaxCategory/cbc:PerUnitAmount`. | [Anexos RS 150-2019](https://www.sunat.gob.pe/legislacion/superin/2019/anexos-150-2019.pdf) | RF-05 |
| Cardinalidad UBL | El XSD de OASIS solo obliga ID, IssueDate, AccountingSupplierParty, AccountingCustomerParty, LegalMonetaryTotal e InvoiceLine; SUNAT exige 22 datos más. | [UBL-Invoice-2.1.xsd](https://docs.oasis-open.org/ubl/os-UBL-2.1/xsd/maindoc/UBL-Invoice-2.1.xsd) | RF-07, RF-08 |
| Códigos de error | Catálogo de códigos de retorno de SUNAT (≈1 700 códigos); se mapean en la matriz §G. | [Greenter CodeErrors.xml](https://github.com/thegreenter/xcodes/blob/master/src/data/CodeErrors.xml) (contrastar con el Excel de validaciones vigente) | RF-10 |
| Validación RUC | 11 dígitos; pesos 5,4,3,2,7,6,5,4,3,2; DV = 11 − (suma mod 11), 10→0, 11→1. | [Oracle docs](https://docs.oracle.com/en/cloud/saas/financials/25d/faitx/peru.html), [laravel-peru-rules](https://packagist.org/packages/aeunius/laravel-peru-rules) | RF-02 |
| Boleta > S/ 700 | Obligatorio tipo y número de documento del Adquirente. | [SUNAT orientación](https://orientacion.sunat.gob.pe/03-boleta-de-venta), [Anexo II RS 114-2019](https://www.sunat.gob.pe/legislacion/superin/2019/anexoII-114-2019.pdf) | RF-04 |
| Forma de pago | Obligatorio Contado/Crédito; si Crédito, monto pendiente, cuotas y fechas. | [RS 193-2020](https://www.sunat.gob.pe/legislacion/superin/2020/193-2020.pdf), [Anexo IV](https://www.sunat.gob.pe/legislacion/superin/2020/anexo4-193-2020.pdf) | RF-12 |
| Plazo de envío factura | Hasta 3 días calendario contados desde el día siguiente a la emisión. | [RS 000003-2023](https://www.sunat.gob.pe/legislacion/superin/2023/000003-2023.pdf) | RF-11, RNF-03 |
| Servicio web | `sendBill` (síncrono, CDR), `sendSummary`/`getStatus` (ticket); WS-Security UsernameToken con usuario y clave SOL. | [Anexo 244-2019](https://ww3.sunat.gob.pe/legislacion/superin/2019/anexo-244-2019.pdf) | RF-10 |
| Nombre de archivo | `RUC-TT-SERIE-NUMERO` (01 factura, 03 boleta, 07 NC, 08 ND). | [Anexo A RS 113-2018](https://ww3.sunat.gob.pe/legislacion/superin/2018/anexoA-113-2018.pdf) | RF-10 |
| Firma | XMLDSig enveloped en ExtensionContent; librerías de referencia usan C14N + RSA-SHA1. | [greenter/xmldsig](https://packagist.org/packages/greenter/xmldsig) | RF-09 |
| Contingencia | Informar comprobantes físicos de contingencia hasta el 7.º día calendario siguiente. | [SUNAT CPE](https://cpe.sunat.gob.pe/informacion_general/procedimiento_contingencia) | RF-16 |
| Conservación | Conservar CPE, CDR y resúmenes al menos 5 años. | [EDICOM](https://edicomgroup.com/es/blog/como-es-la-factura-electronica-peru) | RNF-06 |
| Cambios 2027 | RS 000048-2026 (nuevas validaciones y catálogos) entra en vigor el 1/1/2027 por RS 000143-2026. | [Sovos](https://sovos.com/es/cambios-regulatorios/iva/peru-boleta-venta-electronica-rs-000048-2026-sunat/) | Todos |
