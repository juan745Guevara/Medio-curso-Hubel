# Seguridad

| Activo | Riesgo | Control | Ref. |
|---|---|---|---|
| Certificado digital (.pfx) y su clave | Robo ⇒ emisión fraudulenta a nombre de ABC | Guardado cifrado en el servidor; nunca en clientes ni en el repositorio; solo el Administrador lo carga; alerta 30 días antes de vencer | RNF-05, constitución 15 |
| Usuario y clave SOL / token OSE | Acceso a SUNAT en nombre de ABC | Gestor de secretos o variable de entorno cifrada; nunca en logs | constitución 15 |
| XML firmado y CDR | Alteración ⇒ pérdida de validez | Hash SHA-256 guardado; almacenamiento de solo escritura 5 años | RNF-06 |
| API | Uso no autorizado | JWT con roles Vendedor, Administrador, Contador; HTTPS obligatorio | facturacion-api.yaml |
| Keystore de Android y certificado de firma de código | Instaladores falsificados | Fuera del repositorio; uso solo en el pipeline de release | ADR 0004 |
| Datos del Adquirente | Exposición de datos personales (Ley 29733) | Mínimo necesario; correo opcional | por verificar |

| Rol | Puede |
|---|---|
| Vendedor | Emitir comprobantes, consultar, descargar, reenviar |
| Administrador | Todo lo del Vendedor + notas, bajas, configuración, migración |
| Contador | Registros y kardex (solo lectura) |

`[NEEDS CLARIFICATION]` Matriz de roles definitiva (ver clarificaciones de 002).
