# Trazabilidad HU → RF → CA → Caso de uso → Test

Una fila por criterio de aceptación de todas las features. Nombre de test según convención C# (xUnit) del ADR 0002, con el CA visible. El agente `qa` diseña cada caso; ningún RF pasa a "hecho" sin sus CA verdes.

| Feature | HU | RF | CA | Caso de uso | Test |
|---|---|---|---|---|---|
| 001 | HU-01 | RF-01 | CA-01 | UC-01 | `Ca01_CorrelativoConsecutivoPorSerie()` |
| 001 | HU-01 | RF-01 | CA-02 | UC-01 | `Ca02_CorrelativoNoSeReutiliza()` |
| 001 | HU-01 | RF-02 | CA-03 | UC-03 | `Ca03_RucValido()` |
| 001 | HU-01 | RF-02 | CA-04 | UC-03 | `Ca04_RucConDigitoVerificadorIncorrecto()` |
| 001 | HU-01 | RF-03 | CA-05 | UC-01 | `Ca05_FacturaSinRuc()` |
| 001 | HU-02 | RF-04 | CA-06 | UC-02 | `Ca06_BoletaMayorAS700SinDocumento()` |
| 001 | HU-02 | RF-04 | CA-07 | UC-02 | `Ca07_BoletaMenorAS700SinDocumento()` |
| 001 | HU-03 | RF-05 | CA-08 | UC-01 | `Ca08_IgvDeItemGravado()` |
| 001 | HU-03 | RF-05 | CA-09 | UC-01 | `Ca09_IscAlValorAntesDelIgv()` |
| 001 | HU-03 | RF-05 | CA-10 | UC-01 | `Ca10_IscMontoFijo()` |
| 001 | HU-03 | RF-05 | CA-11 | UC-01 | `Ca11_IcbperFueraDeLaBaseDelIgv()` |
| 001 | HU-03 | RF-05 | CA-12 | UC-01 | `Ca12_ItemExonerado()` |
| 001 | HU-03 | RF-06 | CA-13 | UC-01 | `Ca13_TotalesDelComprobante()` |
| 001 | HU-01 | RF-07 | CA-14 | UC-01 | `Ca14_EncabezadoUbl()` |
| 001 | HU-01 | RF-08 | CA-15 | UC-01 | `Ca15_XmlInvalidoNoSeEnvia()` |
| 001 | HU-01 | RF-09 | CA-16 | UC-01 | `Ca16_FirmaPresente()` |
| 001 | HU-04 | RF-10 | CA-17 | UC-01 | `Ca17_CdrAceptado()` |
| 001 | HU-04 | RF-10 | CA-18 | UC-01 | `Ca18_CdrRechazado()` |
| 001 | HU-04 | RF-11 | CA-19 | UC-08 | `Ca19_SinConexion()` |
| 001 | HU-04 | RF-11 | CA-20 | UC-08 | `Ca20_AlertaDePlazo()` |
| 001 | HU-01 | RF-12 | CA-21 | UC-01 | `Ca21_CreditoSinCuotas()` |
| 001 | HU-01 | RF-12 | CA-22 | UC-01 | `Ca22_CuotasNoCuadran()` |
| 001 | HU-05 | RF-13 | CA-23 | UC-09 | `Ca23_RepresentacionImpresa()` |
| 001 | HU-05 | RF-14 | CA-24 | UC-09 | `Ca24_EnvioPorCorreo()` |
| 001 | HU-01 | RF-15 | CA-25 | UC-01 | `Ca25_LeyendaEnLetras()` |
| 001 | HU-06 | RF-16 | CA-26 | UC-10 | `Ca26_ContingenciaFueraDePlazo()` |
| 001 | HU-01 | RF-17 | CA-27 | UC-01 | `Ca27_SalidaDeInventario()` |
| 002 | HU-07 | RF-18 | CA-28 | UC-04 | `Ca28_NotaSinMotivo()` |
| 002 | HU-07 | RF-18 | CA-29 | UC-04 | `Ca29_DevolucionPorItem()` |
| 002 | HU-07 | RF-19 | CA-30 | UC-04 | `Ca30_NotaSobreComprobanteRechazado()` |
| 002 | HU-07 | RF-20 | CA-31 | UC-04 | `Ca31_SerieDeLaNota()` |
| 002 | HU-07 | RF-21 | CA-32 | UC-04 | `Ca32_NotaSobreDosComprobantes()` |
| 002 | HU-07 | RF-22 | CA-33 | UC-04 | `Ca33_ExcesoDeCredito()` |
| 002 | HU-09 | RF-23 | CA-34 | UC-06 | `Ca34_BajaAceptada()` |
| 002 | HU-09 | RF-24 | CA-35 | UC-06 | `Ca35_BajaFueraDePlazo()` |
| 002 | HU-10 | RF-25 | CA-36 | UC-07 | `Ca36_ResumenDe620Boletas()` |
| 002 | HU-10 | RF-26 | CA-37 | UC-07 | `Ca37_TicketEnProceso()` |
| 002 | HU-11 | RF-27 | CA-38 | UC-08 | `Ca38_ReenvioManual()` |
| 003 | HU-12 | RF-28 | CA-39 | UC-13 | `Ca39_LoteRegistrado()` |
| 003 | HU-12 | RF-29 | CA-40 | UC-13 | `Ca40_Entrada()` |
| 003 | HU-13 | RF-30 | CA-41 | UC-13 | `Ca41_TrazabilidadALaFila()` |
| 003 | HU-12 | RF-31 | CA-42 | UC-13 | `Ca42_ProductoNuevo()` |
| 003 | HU-14 | RF-32 | CA-43 | UC-13 | `Ca43_FichaInvalida()` |
| 003 | HU-14 | RF-33 | CA-44 | UC-13 | `Ca44_ReintentoSinDuplicados()` |
| 003 | HU-13 | RF-34 | CA-45 | UC-13 | `Ca45_Conciliacion()` |
| 003 | HU-13 | RF-35 | CA-46 | UC-13 | `Ca46_NroDocumentoNoEsComprobante()` |
| 003 | HU-12 | RF-36 | CA-47 | UC-12 | `Ca47_ProductoSinClasificar()` |
| 004 | HU-15 | RF-37 | CA-48 | UC-14 | `Ca48_RegistroDelPeriodo()` |
| 004 | HU-15 | RF-38 | CA-49 | UC-14 | `Ca49_NotaDeCreditoResta()` |
| 004 | HU-15 | RF-39 | CA-50 | UC-14 | `Ca50_RechazadoExcluido()` |
| 004 | HU-16 | RF-40 | CA-51 | UC-15 | `Ca51_KardexConMigrados()` |
| 004 | HU-15 | RF-41 | CA-52 | UC-14 | `Ca52_Exportacion()` |

Total: 52 CA.
