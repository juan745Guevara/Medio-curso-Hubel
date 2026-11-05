# Evidence: invoice accepted by SUNAT's test service (beta)

Produced by `SunatBetaTests.Ac17_sunat_beta_accepts_the_invoice` (AC-17, FR-10, FR-52) on 2026-11-04 from the
student's PC, with `SUNAT_BETA=1`. The test runs the whole C# flow: generate the XML from the classes of the
UBL 2.1 XSD, validate it against the official XSD and SUNAT's rules, sign it, send it with `sendBill`.

| File | What it is |
|---|---|
| [`20131312955-01-F001-93840690.xml`](20131312955-01-F001-93840690.xml) | Signed invoice sent to `https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService` |
| [`R-20131312955-01-F001-93840690.zip`](R-20131312955-01-F001-93840690.zip) | CDR returned by SUNAT (ApplicationResponse signed by SUNAT) |
| [`R-20131312955-01-F001-93840690.txt`](R-20131312955-01-F001-93840690.txt) | Summary: `ResponseCode 0` — "La Factura numero F001-93840690, ha sido aceptada" |

The beta has no tax effect: SUNAT's public test credentials (`RUC` + `MODDATOS` / `moddatos`) and a self-signed test
certificate generated in memory (only its public part is in the XML; constitution 15). The sequence number comes from
the clock so every run is new. The signed XML also validates with lxml against the OASIS XSD and its signature
verifies with libxmlsec1.

To repeat it: `$env:SUNAT_BETA="1"; dotnet test --filter "FullyQualifiedName~SunatBetaTests"` (PowerShell).
