# Spec 003 — Migrar datos del legado

## Contexto
El software actual guarda Fichas legado con fecha, ingreso, salida, producto y nro_documento. La gerencia exige que toda esa historia pase a las nuevas entidades "sin perder trazabilidad". Esos registros no son Comprobantes y no pueden convertirse en ellos (constitución, invariante 12).

## Actores
- **Administrador**: ejecuta y aprueba la migración.
- **Auditor**: revisa el reporte de migración.

## Historias de usuario
- HU-12: Como Administrador, quiero importar el historial del almacén para no perder información.
- HU-13: Como Auditor, quiero rastrear cada registro nuevo hasta su fila de origen.
- HU-14: Como Administrador, quiero ver qué filas se rechazaron y por qué, para corregirlas y reintentar.

## Requisitos funcionales
- **RF-28** (Evento): Cuando el Administrador cargue un archivo de exportación del legado, el sistema deberá crear un Lote de migración con fecha, usuario, nombre del archivo, número de filas y hash del archivo.
- **RF-29** (Siempre): El sistema deberá convertir cada Ficha legado válida en un Movimiento de inventario: tipo ENTRADA si ingreso > 0, SALIDA si salida > 0, con la fecha y el producto de la ficha.
- **RF-30** (Siempre): El sistema deberá guardar en cada Movimiento migrado el identificador del Lote de migración, el número de fila de origen y el contenido original de la Ficha legado.
- **RF-31** (Evento): Cuando un producto de la Ficha legado no exista en el catálogo de Productos, el sistema deberá crearlo en estado PENDIENTE_CLASIFICACION, sin Afectación IGV asignada, para que el Administrador la complete.
- **RF-32** (No deseado): Si una Ficha legado tiene fecha inválida, producto vacío, ingreso y salida ambos en cero o ambos mayores que cero, o valores negativos, entonces el sistema deberá rechazarla y registrar el motivo.
- **RF-33** (No deseado): Si una Ficha legado ya fue migrada en un Lote anterior (mismo contenido y fila), entonces el sistema deberá omitirla y contarla como duplicada.
- **RF-34** (Siempre): El sistema deberá producir un reporte de conciliación por Lote: filas leídas = migradas + rechazadas + duplicadas, y saldo por Producto calculado desde el legado igual al saldo migrado.
- **RF-35** (Siempre): El sistema deberá conservar nro_documento como referencia interna del Movimiento, nunca como Serie ni Correlativo de un Comprobante.
- **RF-36** (Estado): Mientras un Producto esté en PENDIENTE_CLASIFICACION, el sistema deberá impedir usarlo en un Comprobante.

## Requisitos no funcionales
- **RNF-10**: La migración de 100 000 Fichas legado deberá completarse en ≤ 10 minutos.
- **RNF-11**: Una migración interrumpida deberá poder reanudarse sin duplicar filas.

## Criterios de aceptación
CA-39 (RF-28) — Lote registrado
  Dado un archivo de 1 200 filas
  Cuando el Administrador lo carga
  Entonces existe un Lote de migración con 1 200 filas y el hash del archivo

CA-40 (RF-29) — Entrada
  Dada la ficha {fecha 2025-03-04, ingreso 50, salida 0, producto "Arroz Costeño 5kg", nro_documento "G-0091"}
  Cuando se migra
  Entonces se crea un Movimiento ENTRADA de 50 del Producto "Arroz Costeño 5kg" con fecha 2025-03-04

CA-41 (RF-30) — Trazabilidad a la fila
  Dado el Movimiento creado en CA-40
  Cuando el Auditor consulta su origen
  Entonces ve el Lote, la fila 17 y los cinco campos originales

CA-42 (RF-31) — Producto nuevo
  Dado un producto "Aceite Primor 1L" que no existe
  Cuando se migra su ficha
  Entonces el Producto se crea en PENDIENTE_CLASIFICACION

CA-43 (RF-32) — Ficha inválida
  Dada una ficha con ingreso 10 y salida 5 en la misma fila
  Cuando se migra
  Entonces se rechaza con motivo "ingreso y salida simultáneos"

CA-44 (RF-33) — Reintento sin duplicados
  Dado un Lote ya migrado
  Cuando el Administrador carga el mismo archivo
  Entonces 0 filas nuevas se migran y todas se cuentan como duplicadas

CA-45 (RF-34) — Conciliación
  Dado un Lote de 1 200 filas con 1 180 migradas y 20 rechazadas
  Cuando se genera el reporte
  Entonces 1 180 + 20 + 0 = 1 200 y el saldo de cada Producto coincide con el calculado desde el legado

CA-46 (RF-35) — nro_documento no es comprobante
  Dada una ficha con nro_documento "F001-123"
  Cuando se migra
  Entonces se guarda como referencia interna y no existe Comprobante F001-00000123

CA-47 (RF-36) — Producto sin clasificar
  Dado un Producto en PENDIENTE_CLASIFICACION
  Cuando el Vendedor lo agrega a una Factura
  Entonces el sistema lo bloquea con "Producto sin afectación tributaria"

## Fuera de alcance
- Reconstruir ventas históricas como Comprobantes o regularizar periodos pasados ante SUNAT.
- Migrar precios o clientes (el enunciado no dice que existan en el legado).

## Clarificaciones
- [NEEDS CLARIFICATION] ¿En qué formato se puede exportar el legado (CSV, Excel, base de datos)?
- [NEEDS CLARIFICATION] ¿"ingreso" y "salida" son cantidades o importes? ¿En qué unidad?
- [NEEDS CLARIFICATION] ¿"producto" es un nombre libre o un código? ¿Hay nombres escritos de varias formas para el mismo producto?
- [NEEDS CLARIFICATION] ¿nro_documento es único? ¿Qué representa (guía, boleta física, vale interno)?
- [NEEDS CLARIFICATION] ¿Cuántos años de historia hay y cuántas filas aproximadamente?
