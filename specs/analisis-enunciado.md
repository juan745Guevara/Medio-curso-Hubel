# Análisis del enunciado

Fuente: Examen de medio curso de Arquitectura de Software, Grupo A, UNAS, 19/10/2026. Solo se extrae lo que dice el texto; lo que falta va a Clarificaciones.

| Categoría | Elementos encontrados | Origen en el texto |
|---|---|---|
| Actores | ABC Software Corp (cliente, mayorista/minorista de abarrotes), gerencia, líder del equipo de desarrollo, SUNAT, Operador de Servicios Electrónicos (OSE), usuarios del almacén | §1 "Ha sido contratado por ABC Software Corp…", §2 "La gerencia…" |
| Entidades | Ficha de almacén (fecha, ingreso, salida, producto, número de documento interno), comprobante de pago electrónico, RUC, impuestos (IGV, ISC, ICBPER), XML UBL, firma digital, registros formales, catálogos SUNAT (tipos de documento, afectación al IGV, códigos de moneda), series y correlativos | §1 párrafos 1-2, §2 "Normativa Tributaria Peruana" |
| Acciones | Registrar movimientos de almacén; emitir comprobantes legales estructurados; validar RUC; calcular impuestos; generar XML; firmar digitalmente; comunicarse con OSE o SUNAT; llevar registros formales; migrar datos históricos | §1 párrafo 2, §2 "Legado" |
| Restricciones | "Estricto cumplimiento" de requisitos SUNAT; migración "sin perder trazabilidad"; monolito MVC "limpio"; diseño desacoplado para migrar a Hexagonal/Clean y luego microservicios, "debe verse claramente en los specs"; ejecución nativa en Windows y Android; instalador MSI y APK "obligatoriamente"; Spec-First: reglas, contratos de datos, validaciones e interfaces antes de código | §1 párrafo 3, §2 completo |
| Normativa / estándares | RS N.º 097-2012/SUNAT; RS N.º 234-2006/SUNAT; "normativas modernas de masificación de comprobantes electrónicos"; UBL (Universal Business Language) de OASIS; archivo `UBL-Invoice-2.1.xsd`; catálogo 8 de SUNAT ("punto clave") | §1 párrafos 2-3, §2 "Normativa" |
| Integraciones externas | SUNAT (envío directo) u OSE | §1 párrafo 2 |
| Legado / datos existentes | Software operativo básico: fecha, ingreso, salida, producto, nro_documento; control "bastante informal" | §1 párrafo 1, §2 "Legado" |
| Plataformas y distribución | Windows (MSI) y Android (APK), nativo | §2 "Distribución y Multiplataforma" |
| Entregable esperado | 1) Modelo de Dominio y Contratos (DTOs / Schemas); 2) Diagramas de Arquitectura MVC con visión a Hexagonal; 3) Especificación de Endpoints / Servicios (Contratos API); 4) Matriz de Trazabilidad Normativa con cruces entre datos SUNAT y datos UBL | §4 |
| Casos de uso | Detallar casos posibles revisando opiniones de usuarios de facturación electrónica y software como Nubefact (usuario DEMO) | §3 |
| Problema actual | No valida RUC; no calcula IGV, ISC, ICBPER; no genera XML; no firma; no se comunica con OSE ni SUNAT; sin registros formales | §1 párrafo 2 |
| Vacíos detectados | ¿Envío directo a SUNAT u OSE? ¿Quién provee el certificado digital? ¿Qué es exactamente "ingreso"/"salida" en el legado (cantidad o importe)? ¿El legado identifica productos por nombre o código? ¿Hay precios en el legado? ¿Qué productos están afectos a ISC? ¿Se venden bolsas plásticas? ¿Ventas en moneda extranjera? ¿Ventas al crédito? ¿Cuántos puntos de emisión (series)? ¿Funcionamiento sin internet? ¿Usuarios y roles? ¿Qué libros exige la RS 234-2006 para el régimen de ABC? | → Clarificaciones en cada `spec.md` |
