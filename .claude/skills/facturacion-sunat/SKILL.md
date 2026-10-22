---
name: facturacion-sunat
description: Procedimientos técnicos de facturación electrónica SUNAT — validar RUC, calcular IGV, ISC (catálogo 8), ICBPER y totales, plazos de envío y uso de catálogos del Anexo 8. No define reglas de negocio nuevas; las reglas viven en las specs.
---

# Facturación electrónica SUNAT — procedimientos

Cada procedimiento cita la regla de la spec que implementa. Datos verificados al 22/10/2026; ver fuentes en `specs/features/*/research.md`.

## 1. Validar RUC (RF-02)
1. Quitar espacios; exigir 11 dígitos numéricos.
2. Prefijo en {10, 15, 16, 17, 20}.
3. Suma = Σ dígito[i] × peso[i] para los 10 primeros, pesos `5 4 3 2 7 6 5 4 3 2`.
4. r = 11 − (suma mod 11); si r = 10 → 0; si r = 11 → 1.
5. r debe ser igual al dígito 11.

Ejemplo: 20131312955 → suma 94, 94 mod 11 = 6, 11 − 6 = 5 ✓.
Nota: el módulo 11 valida formato, no existencia ni condición ACTIVO/HABIDO (eso requiere consultar el padrón SUNAT; ver clarificaciones).

## 2. Calcular un Ítem (RF-05)
Orden obligatorio, con decimales exactos (nunca coma flotante):

1. `valorVenta = cantidad × valorUnitario − descuentoItem`
2. ISC según catálogo 08 (`cbc:TierRange`):
   - `01` Sistema al valor: `isc = valorVenta × tasaIsc`
   - `02` Monto fijo: `isc = cantidad × montoFijoPorUnidad`
   - `03` Precio de venta al público: `isc = precioVentaPublicoSugerido × factor × tasaIsc` (fórmula del factor por verificar en el Apéndice IV lit. C y la guía SUNAT)
3. IGV según catálogo 07:
   - `10`–`17` gravado: `igv = (valorVenta + isc) × 0.18`
   - `20`–`21` exonerado, `30`–`37` inafecto, `40` exportación: `igv = 0`
4. ICBPER (si el Producto es bolsa plástica): `icbper = bolsas × montoVigente` (S/ 0.50 desde 2023). No entra en la base del IGV.
5. `importeItem = valorVenta + isc + igv + icbper`
6. `precioUnitario = (valorVenta + isc + igv) / cantidad` (catálogo 16 código `01`).

Comprobación de CA-09: 100.00 × 25 % = 25.00; (100.00 + 25.00) × 18 % = 22.50; total 147.50.

## 3. Totales (RF-06)
- `gravado` = Σ valorVenta de ítems 10–17; `exonerado` = Σ 20–21; `inafecto` = Σ 30–37.
- `igv`, `isc`, `icbper` = Σ de los ítems.
- `importeTotal` = gravado + exonerado + inafecto + igv + isc + icbper − descuentosGlobales.
- Redondeo half-up a 2 decimales al final de cada línea y del total. La tolerancia de SUNAT entre la suma de líneas y el total está en el Excel de validaciones (por verificar).

## 4. Tasa de IGV en 2026
En el XML se consigna 18 (`cbc:Percent`). Internamente, por la Ley 32387: 2026 = 15.5 % IGV + 2.5 % IPM; 2027 = 15 % + 3 %; 2028 = 14.5 % + 3.5 %; 2029 = 14 % + 4 %. El desglose solo afecta reportes, no el XML ni el total.

## 5. Catálogos del Anexo 8 más usados

| Cat. | Para | Valores del alcance |
|---|---|---|
| 01 | Tipo de documento | 01 factura, 03 boleta, 07 NC, 08 ND |
| 02 | Moneda | PEN, USD |
| 03 | Unidad | NIU (unidad), KGM, LTR, ZZ (servicio) |
| 05 | Tributo | 1000 IGV (VAT), 2000 ISC (EXC), 7152 ICBPER (por verificar), 9997 exonerado, 9998 inafecto, 9999 otros |
| 06 | Doc. identidad | 0 otros, 1 DNI, 4 CE, 6 RUC, 7 pasaporte, A céd. diplomática |
| 07 | Afectación IGV | 10 gravado oneroso, 20 exonerado oneroso, 30 inafecto oneroso, 40 exportación |
| **08** | **Sistema ISC** | **01 al valor, 02 monto fijo, 03 precio de venta al público** |
| 09 | Motivo NC | 01–10, 13 |
| 10 | Motivo ND | 01 intereses, 02 aumento de valor, 03 penalidades |
| 16 | Tipo de precio | 01 precio con IGV, 02 valor referencial (gratuitas) |
| 51 | Tipo de operación | 0101 venta interna |
| 52 | Leyendas | 1000 monto en letras |

Cargar los catálogos como datos con fecha de vigencia (constitución 9), no como constantes en el código.

## 6. Plazos (RF-11, RF-16, RF-24, RF-25)
| Documento | Plazo |
|---|---|
| Factura y sus notas | Hasta 3 días calendario contados desde el día siguiente a la emisión |
| Resumen diario de boletas | Hasta el 7.º día calendario siguiente |
| Comunicación de baja | 7 días calendario desde el día siguiente |
| Contingencia | Informar hasta el 7.º día calendario siguiente |

Implementa los plazos en un solo servicio (`PlazosSunat`) que reciba la fecha desde `RelojPort` (zona America/Lima).

## 7. Nombres de archivo
`<RUC>-<TT>-<SERIE>-<NUMERO>.xml`, empaquetado en `.zip` con el mismo nombre. Resumen: `<RUC>-RC-<AAAAMMDD>-<n>`. Baja: `<RUC>-RA-<AAAAMMDD>-<n>`.

## 8. Monto en letras (leyenda 1000)
`<entero en palabras> CON <centavos>/100 <MONEDA>`, en mayúsculas: 79.00 PEN → "SETENTA Y NUEVE CON 00/100 SOLES".
