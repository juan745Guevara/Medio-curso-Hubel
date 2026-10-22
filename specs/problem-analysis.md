# Problem statement analysis

Source: Software Architecture mid-term exam, Group A, UNAS, 19/10/2026. Only what the text says is extracted; anything missing goes to Clarifications.

| Category | Elements found | Where in the text |
|---|---|---|
| Actors | ABC Software Corp (client, wholesale/retail grocery seller), management, development team lead, SUNAT, Electronic Services Operator (OSE), warehouse users | §1 "Ha sido contratado por ABC Software Corp…", §2 "La gerencia…" |
| Entities | Warehouse record (date, inflow, outflow, product, internal document number), electronic payment document, RUC, taxes (IGV, ISC, ICBPER), UBL XML, digital signature, formal registers, SUNAT catalogs (document types, IGV treatment, currency codes), series and sequence numbers | §1 paragraphs 1-2, §2 "Normativa Tributaria Peruana" |
| Actions | Record warehouse movements; issue structured legal documents; validate RUC; calculate taxes; generate XML; sign digitally; communicate with an OSE or SUNAT; keep formal registers; migrate historical data | §1 paragraph 2, §2 "Legado" |
| Constraints | "Strict compliance" with SUNAT requirements; migration "without losing traceability"; "clean" MVC monolith; design decoupled to migrate to Hexagonal/Clean and later microservices, "must be clearly visible in the specs"; native execution on Windows and Android; MSI installer and APK "mandatory"; Spec-First: rules, data contracts, validations and interface contracts before any code | §1 paragraph 3, §2 entire |
| Regulations / standards | RS N.º 097-2012/SUNAT; RS N.º 234-2006/SUNAT; "modern regulations on mass adoption of electronic documents"; UBL (Universal Business Language) by OASIS; file `UBL-Invoice-2.1.xsd`; SUNAT catalog 8 ("key point") | §1 paragraphs 2-3, §2 "Normativa" |
| External integrations | SUNAT (direct submission) or OSE | §1 paragraph 2 |
| Legacy / existing data | Basic operating software: fecha, ingreso, salida, producto, nro_documento; "quite informal" control | §1 paragraph 1, §2 "Legado" |
| Platforms and distribution | Windows (MSI) and Android (APK), native | §2 "Distribución y Multiplataforma" |
| Expected deliverable | 1) Domain Model and Contracts (DTOs / Schemas); 2) MVC Architecture Diagrams with a Hexagonal vision; 3) Endpoint / Service Specification (API Contracts); 4) Regulatory Traceability Matrix crossing SUNAT data and UBL data | §4 |
| Use cases | Detail the possible cases by reviewing e-invoicing user opinions and software such as Nubefact (DEMO user) | §3 |
| Current problem | Does not validate RUC; does not calculate IGV, ISC, ICBPER; does not generate XML; does not sign; does not communicate with an OSE or SUNAT; no formal registers | §1 paragraph 2 |
| Gaps detected | Direct submission to SUNAT or via an OSE? Who provides the digital certificate? What exactly are "ingreso"/"salida" in the legacy data (quantity or amount)? Does the legacy data identify products by name or code? Does the legacy data have prices? Which products are subject to ISC? Are plastic bags sold? Foreign-currency sales? Credit sales? How many points of issue (series)? Offline operation? Users and roles? Which books does RS 234-2006 require for ABC's tax regime? | → Clarifications in each `spec.md` |
