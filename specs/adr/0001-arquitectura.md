---
id: 0001
title: Arquitectura — monolito MVC con núcleo hexagonal
status: aceptada
origen: enunciado
---
# Contexto
El enunciado exige (§2): "construirse inicialmente bajo una arquitectura monolítica limpia utilizando el patrón MVC… desacoplarse de tal forma que permita una fácil migración a futuro hacia una Arquitectura Hexagonal o Clean Architecture, preparándose para escalar a microservicios… este detalle debe verse claramente en los specs". Además hay normativa compleja (RF-05, RF-07 a RF-10) y un integrador externo intercambiable (SUNAT u OSE, RNF-07).

# Decisión
**Monolito MVC con el Modelo organizado como núcleo hexagonal.**

- **V (Vista)**: apps cliente Windows y Android, más vistas Razor de administración en el servidor.
- **C (Controlador)**: controladores del monolito. Solo traducen HTTP ↔ DTO y llaman a un **puerto de entrada**. No contienen reglas.
- **M (Modelo)**: se parte en tres capas con dependencias hacia dentro: `Domain` (entidades, value objects, servicios de dominio), `Application` (casos de uso = puertos de entrada; puertos de salida como interfaces) e `Infrastructure` (adaptadores: base de datos, UBL, XSD, firma, SUNAT/OSE, PDF, correo).

Regla de dependencias (verificada con pruebas de arquitectura):

| Paquete | Puede depender de | No puede depender de |
|---|---|---|
| Domain | nada | Application, Infrastructure, Web, frameworks |
| Application | Domain | Infrastructure, Web |
| Infrastructure | Application, Domain | Web |
| Web (Controllers + Views) | Application | Domain directamente, Infrastructure (solo vía composición) |

Esta regla es la que comparten Hexagonal y Clean Architecture; la equivalencia capa por capa está en [arquitectura.md §4b](../architecture/arquitectura.md#4b-hexagonal-o-clean-la-misma-regla-de-dependencias).

Así, "migrar a hexagonal" es renombrar Web e Infrastructure como adaptadores, sin tocar Domain ni Application; y "migrar a microservicios" es separar por contextos acotados (Facturación, Catálogo, Inventario, Migración, Registros) que ya no se llaman por dentro sino por puertos y eventos.

# Alternativas evaluadas
| Opción | Ajuste al dominio | RNF | Integraciones | Simplicidad | Resultado |
|---|---|---|---|---|---|
| MVC clásico (modelo = entidades de ORM, lógica en controladores) | Bajo: reglas tributarias mezcladas con HTTP y base de datos | Cumple | Cambiar de SUNAT a OSE toca controladores | Alta | Descartada: viola "desacoplarse" |
| **MVC con núcleo hexagonal** | Alto: dominio puro y testeable | Cumple | Puertos para SUNAT/OSE | Media | **Elegida** |
| Microservicios desde el día 1 | Alto | Sobra para una empresa mediana; más operación | Alto | Baja | Descartada: el enunciado pide monolito inicial |

# Consecuencias
+ El cálculo tributario y las reglas se prueban sin base de datos ni SUNAT.
+ Cambiar de OSE o de SUNAT a OSE es un adaptador nuevo.
+ Las fronteras de microservicios ya existen como contextos acotados.
– Más proyectos y mapeos DTO ↔ dominio que un MVC clásico.
– Requiere disciplina: pruebas de arquitectura que fallen si Domain importa Infrastructure.
