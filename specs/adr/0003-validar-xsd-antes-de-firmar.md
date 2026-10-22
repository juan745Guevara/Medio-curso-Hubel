---
id: 0003
title: Validar contra el XSD UBL 2.1 antes de firmar y enviar
status: aceptada
origen: enunciado
---
# Contexto
El enunciado pide estudiar `UBL-Invoice-2.1.xsd` y cumplir "estrictamente" con SUNAT. Un XML mal formado se rechaza en SUNAT/OSE y consume el correlativo (constitución 6).

# Decisión
Pipeline fijo: reglas de negocio → generar XML → **validar XSD OASIS 2.1 local** → firmar → enviar. Si el XSD falla, el documento vuelve a BORRADOR y no se envía (RF-08). Los XSD se versionan junto al sistema y se descargan del paquete oficial de OASIS.

# Alternativas evaluadas
| Opción | Resultado |
|---|---|
| Confiar solo en la validación de SUNAT/OSE | Descartada: rechazos tardíos y correlativos perdidos |
| Validar con el XSLT de reglas del Facturador SUNAT además del XSD | Recomendada como mejora futura; aumenta la cobertura de reglas de negocio |

# Consecuencias
+ Ningún error de estructura llega a SUNAT.
– Hay que mantener los XSD y, a futuro, las reglas del Excel de validaciones de SUNAT.
