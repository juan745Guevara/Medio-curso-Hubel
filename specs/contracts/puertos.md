# Puertos (contratos de servicio internos y externos)

Entregable 3 (parte "Servicios"). Son las interfaces que hacen posible pasar de MVC a hexagonal sin reescribir el dominio: hoy los controladores MVC llaman a los **puertos de entrada**; los **puertos de salida** se implementan con adaptadores. Firmas en notación lógica, independiente del lenguaje.

## Puertos de entrada (casos de uso)

| Puerto | Operación | Pre | Post | Errores | RF |
|---|---|---|---|---|---|
| `EmitirComprobante` | `ejecutar(EmitirComprobanteRequest) → DocumentoElectronicoResponse` | Serie existe y activa; Productos clasificados; Certificado vigente | Documento con número asignado, XML firmado guardado, envío encolado o resultado CDR | RUC_INVALIDO, FACTURA_REQUIERE_RUC, BOLETA_REQUIERE_DOCUMENTO, CREDITO_SIN_CUOTAS, CUOTAS_NO_CUADRAN, XSD_INVALIDO, PRODUCTO_SIN_CLASIFICAR | RF-01..RF-15 |
| `EmitirNota` | `ejecutar(EmitirNotaRequest) → DocumentoElectronicoResponse` | Documento modificado ACEPTADO/OBSERVADO | Nota emitida | NOTA_MULTIPLE, DOC_NO_ACEPTADO, CREDITO_EXCEDIDO | RF-18..RF-22 |
| `ComunicarBaja` | `ejecutar(ComunicarBajaRequest) → EnvioLoteResponse` | Dentro de plazo | Ticket registrado | BAJA_FUERA_DE_PLAZO | RF-23, RF-24 |
| `GenerarResumenDiario` | `ejecutar(fecha) → EnvioLoteResponse[]` | Hay boletas/notas no informadas | Envíos de ≤ 500 líneas con ticket | — | RF-25 |
| `ConsultarTicket` | `ejecutar(envioId) → EnvioLoteResponse` | Envío con ticket | Estado actualizado | — | RF-26 |
| `ReenviarPendientes` | `ejecutar() → resumen` | — | Reintentos hechos; alertas de plazo | — | RF-11 |
| `ConsultarDocumento` | `ejecutar(id) → DocumentoElectronicoResponse` | — | — | NO_EXISTE | RF-27 |
| `MigrarLote` | `ejecutar(archivo) → LoteMigracionResponse` | — | Movimientos creados, conciliación | — | RF-28..RF-35 |
| `GenerarRegistroVentas` | `ejecutar(periodo) → RegistroVentasFila[]` | — | — | — | RF-37..RF-39 |

## Puertos de salida

| Puerto | Operación | Adaptador inicial | Adaptadores futuros | Notas |
|---|---|---|---|---|
| `DocumentoRepository` | `guardar`, `porId`, `porNumero`, `porEstado`, `delPeriodo` | Base relacional | Servicio de facturación propio | Transacción con `SerieRepository` |
| `SerieRepository` | `siguienteCorrelativo(serie) → NumeroDocumento` (atómico) | Bloqueo por fila | — | Constitución 6 |
| `ProductoRepository`, `AdquirenteRepository`, `MovimientoRepository`, `LoteRepository` | CRUD | Base relacional | — | |
| `CatalogoSunatPort` | `existe(catalogo, codigo, fecha)`, `descripcion(...)` | Tablas cargadas del Anexo 8 | API de catálogos | Vigencia por fecha (constitución 9) |
| `GeneradorUblPort` | `generar(DocumentoElectronico) → XML` | Plantillas UBL 2.1 | — | Mapeo de la [matriz](../traceability/matriz-normativa.md) |
| `ValidadorXsdPort` | `validar(XML, tipo) → errores[]` | XSD OASIS 2.1 locales | — | RF-08 |
| `FirmadorPort` | `firmar(XML) → XMLFirmado, hash` | XMLDSig con certificado local | HSM / firma en la nube | RF-09 |
| `ValidadorSunatPort` | `enviarDocumento(zip, nombre) → Cdr`; `enviarLote(zip, nombre) → ticket`; `consultarTicket(ticket) → Cdr?` | SOAP billService SUNAT | Adaptador por OSE (REST/SOAP del proveedor) | Intercambiable por config (RNF-07) |
| `RepresentacionImpresaPort` | `generarPdf(doc, formato) → PDF` | Generador PDF + QR | — | RF-13 |
| `NotificadorPort` | `enviarCorreo(destino, adjuntos)` | SMTP | Servicio de mensajería | RF-14 |
| `AlmacenDocumentosPort` | `guardar(nombre, bytes) → ruta`, `leer(ruta)` | Disco del servidor | Almacenamiento de objetos | RNF-06, 5 años |
| `RelojPort` | `hoy()`, `ahora()` | Zona America/Lima | — | Cálculo de plazos testeable |
| `EventosPort` | `publicar(evento)` | En memoria | Broker de mensajes | Corte a microservicios |

## Contrato externo: Validador SUNAT (SEE-Del contribuyente)

| Método | Uso | Entrada | Salida | Fuente |
|---|---|---|---|---|
| `sendBill` | Factura, boleta individual, notas | ZIP `RUC-TT-SERIE-NUM.zip` en base64 | ZIP con CDR (síncrono) | [Anexo 244-2019](https://ww3.sunat.gob.pe/legislacion/superin/2019/anexo-244-2019.pdf) |
| `sendSummary` | Resumen diario (RC) y Comunicación de baja (RA) | ZIP en base64 | Ticket | idem |
| `getStatus` | Consultar ticket | Ticket | Estado + CDR | idem |
| Autenticación | WS-Security UsernameToken | RUC+usuario SOL, clave SOL | — | idem |
| Endpoints | Beta: `e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService`; Producción: `e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService` | — | — | [greenter/ws](https://packagist.org/packages/greenterfin/ws) (por verificar en SUNAT) |

Con OSE, el contrato lo define cada operador; el adaptador traduce al mismo `ValidadorSunatPort`.
