# Arquitectura: MVC con visión a Hexagonal

Entregable 2 del examen. Decisiones en [ADR 0001](../adr/0001-arquitectura.md), [ADR 0002](../adr/0002-stack-tecnologico.md), [ADR 0003](../adr/0003-validar-xsd-antes-de-firmar.md) y [ADR 0004](../adr/0004-empaquetado-msi-apk.md).

La idea en una línea: **hoy es un monolito MVC; su "M" ya está construida como un hexágono**, así que la migración futura no reescribe reglas, solo reubica adaptadores.

## 1. C4 nivel 1 — Contexto

```mermaid
flowchart TB
  vend([Vendedor / Administrador / Contador])
  cli([Adquirente])
  sys[Sistema de Facturación ABC]
  sunat[(SUNAT<br/>billService)]
  ose[(OSE)]
  legado[(Software legado<br/>fichas de almacén)]
  mail[(Servidor de correo)]
  vend -- usa --> sys
  sys -- XML UBL firmado / CDR --> sunat
  sys -. alternativa .-> ose
  legado -- exportación única --> sys
  sys -- PDF + XML --> mail --> cli
```

## 2. C4 nivel 2 — Contenedores

```mermaid
flowchart LR
  subgraph Clientes nativos
    win[App Windows<br/>.NET MAUI · MSI]
    and[App Android<br/>.NET MAUI · APK]
    cache[(SQLite local<br/>cola + catálogos)]
  end
  subgraph Servidor ABC
    web[Monolito ASP.NET Core MVC<br/>Controllers + Views admin]
    jobs[Tareas programadas<br/>reenvíos, resumen diario, tickets]
    db[(PostgreSQL)]
    files[(Almacén de XML, CDR, PDF<br/>5 años)]
  end
  win & and -- HTTPS JSON<br/>facturacion-api.yaml --> web
  win & and --- cache
  web --- db
  jobs --- db
  web --- files
  web -- SOAP WS-Security --> sunat[(SUNAT / OSE)]
  jobs -- SOAP --> sunat
```

## 3. Vista inicial: MVC (lo que se construye primero)

```mermaid
flowchart TB
  subgraph V[Vista]
    v1[App MAUI: pantallas de venta, notas, consulta]
    v2[Vistas Razor: configuración, migración, registros]
  end
  subgraph C[Controlador]
    c1[ComprobantesController]
    c2[NotasController]
    c3[BajasController / ResumenesController]
    c4[MigracionesController]
    c5[RegistrosController]
  end
  subgraph M[Modelo]
    direction TB
    app[Application<br/>casos de uso = puertos de entrada]
    dom[Domain<br/>DocumentoElectronico, Item, CalculadoraTributos,<br/>ValidadorRuc, PoliticaEmision, PlazosSunat]
    inf[Infrastructure<br/>Repositorios, GeneradorUbl, ValidadorXsd,<br/>Firmador, ClienteSunat, PDF, Correo]
  end
  V --> C
  C -- DTO --> app
  app --> dom
  inf -. implementa puertos de .-> app
```

Reglas de MVC limpio que exige el diseño:

1. El controlador no calcula, no valida reglas tributarias y no toca la base de datos: mapea DTO y llama a un caso de uso.
2. La vista nunca recibe entidades de dominio; recibe DTO de respuesta.
3. El modelo no conoce HTTP, MAUI, PostgreSQL ni SUNAT.

## 4. Vista objetivo: Hexagonal (puertos y adaptadores)

```mermaid
flowchart LR
  subgraph Adaptadores primarios
    a1[API REST MVC]
    a2[Vistas Razor]
    a3[Tareas programadas]
    a4[Pruebas automáticas]
  end
  subgraph Hexágono
    pin[[Puertos de entrada<br/>EmitirComprobante, EmitirNota,<br/>ComunicarBaja, GenerarResumenDiario,<br/>MigrarLote, GenerarRegistroVentas]]
    core((Dominio<br/>Facturación · Catálogo · Inventario))
    pout[[Puertos de salida<br/>DocumentoRepository, SerieRepository,<br/>GeneradorUbl, ValidadorXsd, Firmador,<br/>ValidadorSunat, Notificador, Reloj]]
    pin --> core --> pout
  end
  subgraph Adaptadores secundarios
    b1[PostgreSQL]
    b2[UBL 2.1 + XSD OASIS]
    b3[XMLDSig + certificado]
    b4[SUNAT billService]
    b5[OSE X]
    b6[SMTP]
  end
  a1 & a2 & a3 & a4 --> pin
  pout --> b1 & b2 & b3 & b4 & b5 & b6
```

Detalle de cada puerto en [`contracts/puertos.md`](../contracts/puertos.md).

## 4b. Hexagonal o Clean: la misma regla de dependencias

El enunciado pide migrar "hacia una Arquitectura Hexagonal o Clean Architecture". Las dos comparten la regla central: las dependencias apuntan hacia el dominio. Este diseño sirve para ambas sin cambiar código del núcleo; solo cambia el nombre de las capas.

| Proyecto propuesto | MVC (hoy) | Hexagonal ([Cockburn](https://alistaircockburn.com/Articles/Hexagonal-Architecture)) | Clean Architecture (Robert C. Martin) | Ejemplos en este sistema |
|---|---|---|---|---|
| `Abc.Facturacion.Domain` | Modelo (núcleo) | Dentro del hexágono: dominio | **Entities** | `DocumentoElectronico`, `Item`, `Ruc`, `Dinero`, `CalculadoraTributos`, `PoliticaEmision` |
| `Abc.Facturacion.Application` | Modelo (servicios) | Puertos de entrada (casos de uso) y definición de puertos de salida | **Use Cases** (interactors) e interfaces de gateways | `EmitirComprobante`, `EmitirNota`, `MigrarLote`; `ValidadorSunatPort`, `FirmadorPort` |
| `Abc.Facturacion.Web` (Controllers) | **Controlador** | Adaptadores primarios (driving) | **Interface Adapters**: controllers y presenters | `ComprobantesController`, mapeo DTO ↔ caso de uso |
| `Abc.Facturacion.Web` (Views), `Abc.Cliente.Maui` | **Vista** | Adaptadores primarios | **Frameworks & Drivers**: UI | Vistas Razor de administración, app Windows/Android |
| `Abc.Facturacion.Infrastructure` | Modelo (acceso a datos) | Adaptadores secundarios (driven) | **Interface Adapters** (gateways, repositorios) + **Frameworks & Drivers** (BD, web) | `PostgresDocumentoRepository`, `GeneradorUbl21`, `ValidadorXsdOasis`, `ClienteSoapSunat`, `ClienteOse` |
| `Abc.Contracts` | DTO de la Vista | Formato de los puertos de entrada | Request / Response models | `EmitirComprobanteRequest`, `DocumentoElectronicoResponse` |

```mermaid
flowchart LR
  subgraph FD[Frameworks & Drivers]
    UI[App MAUI · Vistas Razor]
    DB[(PostgreSQL)]
    SUNAT[(SUNAT / OSE)]
  end
  subgraph IA[Interface Adapters]
    CTRL[Controllers MVC]
    GW[Repositorios · Cliente SUNAT/OSE · Generador UBL · Firmador]
  end
  subgraph UC[Use Cases]
    UCS[EmitirComprobante · EmitirNota · ComunicarBaja · MigrarLote]
  end
  subgraph EN[Entities]
    DOM[DocumentoElectronico · CalculadoraTributos · Ruc · Dinero]
  end
  UI --> CTRL --> UCS --> DOM
  GW -. implementa puertos de .-> UCS
  GW --> DB & SUNAT
```

Las flechas solo cruzan los círculos hacia dentro: `Entities` no conoce `Use Cases`, y `Use Cases` no conoce ni la base de datos ni SUNAT. Es la misma regla de dependencias del [ADR 0001](../adr/0001-arquitectura.md), que una prueba de arquitectura verifica en cada build.

## 5. Ruta de migración

| Paso | De | A | Qué cambia | Qué NO cambia |
|---|---|---|---|---|
| 0 | — | Monolito MVC con núcleo hexagonal | Se construye así desde el inicio | — |
| 1 | Controllers + Infrastructure | Adaptadores primarios y secundarios con nombre explícito | Renombrar proyectos; registrar adaptadores por configuración | Domain, Application, contratos API |
| 2 | Llamadas internas entre contextos | Eventos de dominio (`ComprobanteAceptado` → Inventario) | `EventosPort` pasa de memoria a broker | Casos de uso |
| 3 | Un despliegue | Servicios por contexto: Facturación, Catálogo, Inventario, Registros | Cada contexto con su base de datos y su API | Puertos y DTO |
| 4 | Envío a SUNAT dentro de Facturación | Servicio "Gateway SUNAT/OSE" aislado | Adaptador `ValidadorSunatPort` pasa a cliente HTTP del gateway | Dominio de facturación |

```mermaid
flowchart LR
  m[Monolito MVC<br/>núcleo hexagonal] --> h[Monolito modular<br/>hexagonal explícito] --> e[Contextos con eventos] --> ms[Microservicios<br/>Facturación · Catálogo · Inventario · Registros · Gateway SUNAT]
```

## 6. Secuencia del flujo central (emitir factura)

```mermaid
sequenceDiagram
  autonumber
  actor V as Vendedor
  participant App as App MAUI
  participant C as ComprobantesController
  participant UC as EmitirComprobante
  participant D as Dominio
  participant S as SerieRepository
  participant U as GeneradorUbl
  participant X as ValidadorXsd
  participant F as Firmador
  participant VS as ValidadorSunat
  V->>App: Confirma venta
  App->>C: POST /comprobantes (DTO)
  C->>UC: ejecutar(request)
  UC->>D: PoliticaEmision + ValidadorRuc + CalculadoraTributos
  alt regla violada
    D-->>C: error de negocio
    C-->>App: 422 problem+json
  end
  UC->>S: siguienteCorrelativo(F001)
  UC->>U: generar(documento)
  U-->>UC: XML UBL 2.1
  UC->>X: validar(XML)
  alt XSD inválido
    X-->>UC: errores
    UC-->>C: XSD_INVALIDO (estado BORRADOR)
  end
  UC->>F: firmar(XML)
  F-->>UC: XML firmado + hash
  UC->>VS: sendBill(ZIP)
  alt CDR recibido
    VS-->>UC: CDR (aceptado / observado / rechazado)
  else sin respuesta
    UC-->>UC: PENDIENTE_REENVIO + reintento
  end
  UC-->>C: DocumentoElectronicoResponse
  C-->>App: 201 + estado + enlaces PDF/XML/CDR
```

## 7. Estructura de carpetas propuesta (descrita, no creada en esta fase)

```
src/
  Abc.Facturacion.Domain/          # entidades, value objects, servicios de dominio — sin dependencias
  Abc.Facturacion.Application/     # casos de uso (puertos de entrada) e interfaces de puertos de salida
  Abc.Facturacion.Infrastructure/  # EF Core/PostgreSQL, UBL, XSD, XMLDSig, cliente SUNAT/OSE, PDF, SMTP
  Abc.Facturacion.Web/             # ASP.NET Core MVC: Controllers, Views (admin), composición DI
  Abc.Contracts/                   # DTO generados desde specs/contracts/schemas (compartidos)
  Abc.Cliente.Maui/                # app Windows (MSI) y Android (APK)
  Abc.Instalador.Wix/              # proyecto WiX para el MSI
tests/
  Abc.Facturacion.Domain.Tests/        # un test por CA de cálculo y reglas
  Abc.Facturacion.Architecture.Tests/  # regla de dependencias del ADR 0001
  Abc.Facturacion.Integration.Tests/   # XSD, firma, SUNAT beta
```

## 8. Qué RF cubre cada componente

| Componente | RF |
|---|---|
| CalculadoraTributos | RF-05, RF-06 |
| ValidadorRuc, PoliticaEmision | RF-02, RF-03, RF-04, RF-12, RF-36 |
| SerieRepository | RF-01 |
| GeneradorUbl + ConversorMontoLetras | RF-07, RF-15 |
| ValidadorXsd | RF-08 |
| Firmador | RF-09 |
| ValidadorSunat + Tareas programadas | RF-10, RF-11, RF-23, RF-25, RF-26 |
| PoliticaNotas | RF-18 a RF-22 |
| RepresentacionImpresa + Notificador | RF-13, RF-14 |
| Migración | RF-28 a RF-35 |
| Registros | RF-37 a RF-41 |
