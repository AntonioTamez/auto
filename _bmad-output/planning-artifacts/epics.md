---
stepsCompleted: [1, 2, 3]
inputDocuments:
  - '{planning_artifacts}/prds/prd-auto-2026-08-19/prd.md'
  - '{planning_artifacts}/architecture/architecture-auto-2026-08-19/ARCHITECTURE-SPINE.md'
  - '{planning_artifacts}/ux-designs/ux-auto-2026-08-19/DESIGN.md'
  - '{planning_artifacts}/ux-designs/ux-auto-2026-08-19/EXPERIENCE.md'
---

# [Proyecto Auto] - Epic Breakdown

## Overview

Este documento descompone en épicas e historias implementables los requerimientos del PRD, el contrato de UX (DESIGN.md + EXPERIENCE.md) y las decisiones de Architecture Spine para [Proyecto Auto] — MVP de comparación de vehículos nuevos para Monterrey.

## Requirements Inventory

### Functional Requirements

- FR-1: Ubicar agencias de vehículos filtrando por ciudad, estado y país.
- FR-2: Mostrar, para cada agencia, las marcas de vehículos que comercializa.
- FR-3: Listar los vehículos disponibles por agencia.
- FR-4: Filtrar vehículos por año.
- FR-5: Filtrar vehículos por precio.
- FR-6: Filtrar vehículos por tipo de carrocería.
- FR-7: Filtrar vehículos por transmisión y tipo de motor/combustible.
- FR-8: Filtrar vehículos por nivel de equipamiento/versión.
- FR-9: Filtrar vehículos por color y disponibilidad estimada.
- FR-10: Seleccionar dos o más vehículos (hasta 3) y compararlos lado a lado.
- FR-11: Guardar una comparación para consultarla después (requiere cuenta).
- FR-12: Compartir una comparación mediante link público de solo lectura.
- FR-13: Disponibilidad en web y app móvil con paridad funcional.
- FR-14: Herramienta interna para cargar y actualizar agencias, marcas y vehículos (backoffice).
- FR-15: Crear cuenta simple (email o teléfono).
- FR-16: Iniciar sesión para acceder a comparaciones guardadas.

### NonFunctional Requirements

- NFR-1: Frescura de datos — catálogo actualizado semanalmente; disponibilidad siempre etiquetada como estimada, nunca tiempo real.
- NFR-2: Búsqueda/filtrado responde en menos de 2 segundos.
- NFR-3: Paridad funcional entre web y móvil.
- NFR-4: Disponibilidad objetivo de 99% en producción.
- NFR-5: Escalabilidad geográfica — el diseño de datos no debe hardcodear Monterrey; debe permitir agregar ciudades/estados sin rediseño mayor.

### Additional Requirements

- No hay starter/template greenfield — la solución backend (.NET/ASP.NET Core, Clean Architecture) se estructura directamente en `Domain/Application/Infrastructure/Api` desde la Historia 1 (Architecture Spine → Structural Seed).
- CI/CD (GitHub Actions: build + test en cada push/PR) debe existir desde el primer endpoint — ninguna historia de backend se considera "hecha" sin pasar por el pipeline (AD-17).
- Infraestructura como código vía Terraform (`azurerm`); resource groups de dev se crean/destruyen bajo demanda vía `workflow_dispatch`, nunca por CLI local (AD-18).
- Contrato REST definido por spec OpenAPI generada desde `Application`/`Api` (ASP.NET Core), publicada y versionada — Angular y Flutter se construyen y validan contra esa spec (AD-16).
- Autenticación: OTP solo por email en el MVP (Azure Communication Services no cubre SMS a México); JWT emitido por endpoints centralizados; almacenamiento seguro de tokens en ambos clientes (AD-4).
- Acceso a `SavedComparison` con verificación de dueño centralizada — un único mecanismo (filtro global EF Core o handler de autorización), nunca filtrado ad hoc por endpoint (AD-5).
- `Agency` con campos estructurados `city`/`state`/`country` desde el MVP (AD-6, soporta NFR-5).
- Links de comparación compartida usan un token público no secuencial, distinto del id interno (AD-7).
- Tope de 3 vehículos por comparación validado en API y reforzado con trigger de Postgres (AD-8).
- `Vehicle` con timestamp de frescura autoritativo en servidor; badge de disponibilidad siempre deriva de ese campo (AD-9).
- `SavedComparison`/`ComparisonVehicle` almacenan snapshot congelado al momento de guardar; catálogo nunca borra en duro, usa `is_active` (AD-10).
- Backoffice (FR-14) es una sección de la misma app Angular, gated por rol — nunca una app separada; staff autentica por el mismo flujo OTP con campo `role` (AD-11).
- Índices de base de datos en cada columna filtrable para cumplir NFR-2; tensión conocida y no resuelta con el cold-start de Container Apps en modo consumo (AD-12).
- `AGENCY_BRAND` es un hecho derivado del inventario de `Vehicle`, nunca curado independientemente (AD-13).
- Una comparación guardada es inmutable en su composición una vez que se emite un link compartido (AD-14).
- `SavedComparison` puede crearse anónima (owner nulo) y ser reclamada por un usuario al guardar — nunca dos filas separadas para "comparar" y "guardar" (AD-15).
- Angular (web) y Flutter (móvil) son codebases independientes sin código de UI compartido — la paridad (NFR-3) se verifica contra el contrato de API, no entre sí (AD-2).
- Los tokens de `DESIGN.md` se implementan de forma independiente en Angular (`definePreset` de PrimeNG) y Flutter (`ThemeData`), cada uno verificado contra `DESIGN.md` directamente (AD-3).
- Datos semilla/fixtures para un ambiente de dev recién creado (vía Terraform) deben respetar AD-13 — no diferido, pero mecanismo exacto pendiente de decidir en la historia correspondiente.

### UX Design Requirements

- UX-DR1: Implementar los tokens de `DESIGN.md` (color, tipografía, spacing, radios) en Angular vía `definePreset` de PrimeNG (preset Aura) con `darkModeSelector` basado en clase.
- UX-DR2: Implementar los mismos tokens de `DESIGN.md` en Flutter (`ThemeData`/`ColorScheme`), verificados independientemente contra `DESIGN.md`, no contra la implementación de Angular.
- UX-DR3: Construir componente `vehicle-result-card` (anatomía, estados, tap-target de comparar independiente del resto de la fila).
- UX-DR4: Construir componente `filter-chip` con el patrón de interacción completo: selector acotado por dimensión (popover en escritorio, hoja inferior en móvil) + chip fijo "Todos los filtros" con drawer.
- UX-DR5: Construir componente `compare-select-button` (toggle agregar/quitar, estado deshabilitado al llegar a 3/3 con caption "Máximo 3 — quita uno primero").
- UX-DR6: Construir componente `compare-footer-bar` (aparece con ≥1 seleccionado, CTA "Comparar" habilitado solo con ≥2).
- UX-DR7: Construir componente `comparison-table` en dos variantes: grilla de escritorio (hasta 3 columnas + etiqueta) y tarjetas apiladas en móvil (Propuesta B elegida), con acción de quitar por columna/tarjeta, solo lectura en vista compartida.
- UX-DR8: Construir componente `availability-badge` — siempre con calificador "estimada" o fecha relativa de actualización, nunca solo.
- UX-DR9: Construir componente `account-form-field` (un solo campo email/teléfono + confirmación, validación inline al perder foco).
- UX-DR10: Construir `button-primary`/`button-secondary` respetando la regla de un solo primario visible por pantalla.
- UX-DR11: Construir `empty-state-panel` en sus dos variantes (sin resultados / sin comparaciones guardadas), cada uno con su CTA específico.
- UX-DR12: Implementar las 7 superficies de la Arquitectura de Información: Búsqueda/Resultados, Detalle de vehículo, Perfil de agencia, Comparación, Crear cuenta/Iniciar sesión, Mis comparaciones guardadas, Comparación compartida (pública).
- UX-DR13: Implementar los patrones de estado (vacío, carga fría, error, frescura de dato) por superficie, según la tabla State Patterns de `EXPERIENCE.md`.
- UX-DR14: Implementar el patrón de interacción de filtros completo (recalculo de resultados al cerrar cualquier selector, sin botón "Aplicar", cumpliendo NFR-2).
- UX-DR15: Implementar el microcopy de marca (tabla Voice and Tone Do/Don't) verbatim en ambos clientes.
- UX-DR16: Piso de accesibilidad WCAG 2.1 AA — pares de contraste verificados, targets táctiles ≥44×44px, foco visible con `{colors.accent}`, encabezados semánticos en `comparison-table` (`scope="col"`/`scope="row"`), anuncio completo de `availability-badge` a lector de pantalla.
- UX-DR17: Comportamiento responsive por breakpoint (forma de `filter-chip`, `compare-footer-bar` fija vs. inline, `comparison-table` grilla vs. tarjetas apiladas).
- UX-DR18: Implementar los 3 Key Flows como jornadas de extremo a extremo verificables: Flujo 1 (Laura busca y compara), Flujo 2 (Diego guarda y crea cuenta al vuelo), Flujo 3 (Ana ve una comparación compartida sin cuenta).

### FR Coverage Map

- FR-1: Epic 3 — descubrimiento, ubicar agencias por ciudad/estado/país
- FR-2: Epic 3 — descubrimiento, marcas por agencia
- FR-3: Epic 3 — descubrimiento, listar vehículos por agencia
- FR-4: Epic 3 — filtrar por año
- FR-5: Epic 3 — filtrar por precio
- FR-6: Epic 3 — filtrar por carrocería
- FR-7: Epic 3 — filtrar por transmisión/motor
- FR-8: Epic 3 — filtrar por equipamiento/versión
- FR-9: Epic 3 — filtrar por color y disponibilidad estimada
- FR-10: Epic 4 — comparar hasta 3 vehículos
- FR-11: Epic 5 — guardar comparación
- FR-12: Epic 5 — compartir comparación (link público)
- FR-13: Epic 6 — paridad funcional móvil (Flutter)
- FR-14: Epic 2 — backoffice de catálogo
- FR-15: Epic 5 — crear cuenta
- FR-16: Epic 5 — iniciar sesión

## Epic List

### Epic 1: Fundación técnica — scaffold desplegable
El equipo tiene un esqueleto de proyecto (.NET con Clean Architecture + Angular) desplegado en Azure, con CI/CD (GitHub Actions) y Terraform funcionando de punta a punta desde un primer endpoint de *health check* — cada épica siguiente se despliega incrementalmente sobre esta base ya automatizada, sin deuda de infraestructura pendiente.
**FRs covered:** Ninguno directamente — habilita AD-17 (CI/CD desde el primer endpoint), AD-18 (Terraform, resource groups de dev efímeros) y la estructura Clean Architecture del Architecture Spine.

### Epic 2: Catálogo — administración interna
El equipo interno puede cargar y mantener agencias y vehículos nuevos (las marcas se derivan del inventario, AD-13) — la base de datos real que todo lo demás consume.
**FRs covered:** FR-14

### Epic 3: Descubrimiento de vehículos
Un comprador ubica agencias por ubicación, ve qué marcas manejan, lista sus vehículos y los filtra por año, precio, carrocería, transmisión, equipamiento, color y disponibilidad.
**FRs covered:** FR-1, FR-2, FR-3, FR-4, FR-5, FR-6, FR-7, FR-8, FR-9

### Epic 4: Comparación de vehículos
Un comprador selecciona hasta 3 vehículos y los ve comparados lado a lado (o apilados en móvil) para decidir con más información — el diferenciador central del producto.
**FRs covered:** FR-10

### Epic 5: Cuenta, guardar y compartir
Un comprador crea una cuenta simple, guarda una comparación para consultarla después, y comparte un link público de solo lectura — sin fricción de cuenta salvo al momento de guardar.
**FRs covered:** FR-11, FR-12, FR-15, FR-16

### Epic 6: Paridad móvil (Flutter)
Todo lo construido en las épicas 2 a 5 queda disponible también en la app móvil, con el mismo comportamiento e identidad visual — como codebase independiente (AD-2), no un port automático.
**FRs covered:** FR-13

## Epic 1: Fundación técnica — scaffold desplegable

El equipo tiene un esqueleto de proyecto (.NET con Clean Architecture + Angular) desplegado en Azure, con CI/CD (GitHub Actions) y Terraform funcionando de punta a punta desde un primer endpoint de health check — cada épica siguiente se despliega incrementalmente sobre esta base ya automatizada, sin deuda de infraestructura pendiente. Relacionado: NFR-4, AD-16, AD-17, AD-18.

### Story 1.1: Esqueleto del repositorio (Clean Architecture + Angular)

Como desarrollador,
quiero un esqueleto de backend en Clean Architecture (`Domain/Application/Infrastructure/Api`) y un shell de Angular en el mismo repo,
para que toda historia futura tenga una estructura consistente donde construir.

**Acceptance Criteria:**

**Given** un repositorio vacío
**When** se hace scaffold del backend y del frontend
**Then** ambos proyectos compilan localmente sin errores
**And** la estructura de carpetas del backend coincide con el Structural Seed del Architecture Spine (Domain sin dependencias hacia afuera)

### Story 1.2: Infraestructura Azure definida en Terraform

Como desarrollador,
quiero la infraestructura de Azure (Container Apps, Static Web Apps, Postgres Flexible Server, Blob Storage, Communication Services, resource group) definida en Terraform,
para poder crear y destruir un ambiente completo sin usar el portal.

**Acceptance Criteria:**

**Given** el código Terraform en el repo
**When** se ejecuta `terraform apply`
**Then** se crea un resource group con los 5 recursos de Azure definidos en el spine, todos con nombres/tags que identifican el ambiente
**And** `terraform destroy` elimina el resource group completo sin dejar recursos huérfanos

### Story 1.3: CI — build y test en cada push/PR

Como desarrollador,
quiero que cada push o PR dispare automáticamente build y test de backend y frontend,
para que código roto nunca llegue silenciosamente a producción.

**Acceptance Criteria:**

**Given** un push a cualquier rama o un PR abierto
**When** se dispara el workflow de GitHub Actions
**Then** se compilan y corren las pruebas de `.NET` y Angular
**And** un PR no puede fusionarse si el pipeline falla (AD-17)

### Story 1.4: CD manual — desplegar el esqueleto a un ambiente de dev

Como desarrollador,
quiero disparar manualmente (`workflow_dispatch`) un despliegue completo del esqueleto a un ambiente de dev,
para validar que todo el pipeline funciona de punta a punta antes de construir features reales.

**Acceptance Criteria:**

**Given** el pipeline de CI en verde
**When** se dispara manualmente el workflow de despliegue
**Then** se ejecuta `terraform apply` y se despliega el esqueleto (API + Angular) al resource group de dev recién creado
**And** nunca se ejecuta `terraform apply`/`destroy` desde una CLI local (AD-18)

### Story 1.5: Endpoint de health-check verificable de punta a punta

Como desarrollador,
quiero un endpoint `/health` desplegado y alcanzable, y que el shell de Angular lo consuma y muestre su estado,
para tener prueba concreta de que código → CI → Terraform → Azure → app corriendo funciona completo.

**Acceptance Criteria:**

**Given** el esqueleto desplegado en dev (Historia 1.4)
**When** se accede a `/health`
**Then** responde 200 con un payload simple de estado
**And** el shell de Angular desplegado hace la llamada y muestra visualmente el estado — confirmando la cadena end-to-end

### Story 1.6: Destruir el ambiente de dev bajo demanda

Como desarrollador,
quiero destruir el ambiente de dev con un solo disparo manual,
para no pagar infraestructura ociosa entre sesiones de trabajo.

**Acceptance Criteria:**

**Given** un ambiente de dev desplegado
**When** se dispara manualmente el workflow de destrucción
**Then** `terraform destroy` elimina el resource group de dev completo
**And** el workflow nunca puede apuntar accidentalmente a staging/producción (AD-18)

### Story 1.7: Publicar el contrato OpenAPI

Como desarrollador,
quiero que la API genere y publique su spec OpenAPI automáticamente,
para que Angular (y después Flutter) siempre construyan contra un contrato versionado y preciso, no contra suposiciones.

**Acceptance Criteria:**

**Given** el endpoint `/health` ya expuesto
**When** se genera la spec OpenAPI desde `Application`/`Api`
**Then** queda publicada y accesible como artefacto versionado del pipeline (AD-16)

### Story 1.8: Monitoreo básico de disponibilidad

Como desarrollador,
quiero que la API y el frontend tengan monitoreo básico de disponibilidad conectado desde el scaffold,
para saber si el sistema está cumpliendo el objetivo de 99% de uptime (NFR-4) en vez de descubrirlo por reportes de usuarios.

**Acceptance Criteria:**

**Given** el esqueleto desplegado en un ambiente (Historia 1.4)
**When** Application Insights está conectado a la API y al frontend, provisionado vía Terraform
**Then** se registran métricas de disponibilidad, latencia y errores
**And** existe una alerta básica que notifica cuando la disponibilidad cae bajo el umbral de NFR-4

## Epic 2: Catálogo — administración interna

El equipo interno puede cargar y mantener agencias y vehículos nuevos (las marcas se derivan del inventario, AD-13) — la base de datos real que todo lo demás consume. Relacionado: FR-14, AD-6, AD-9, AD-11, AD-13.

### Story 2.1: Alta de agencia

Como miembro del equipo de curación,
quiero dar de alta una agencia con su ciudad/estado/país,
para empezar a construir el catálogo de una ubicación.

**Acceptance Criteria:**

**Given** una sesión con rol admin
**When** se crea una agencia con nombre y ciudad/estado/país
**Then** se guarda con esos campos estructurados y consultables (AD-6)
**And** un usuario sin rol admin no puede acceder a esta sección (AD-11)

### Story 2.2: Editar datos de una agencia

Como miembro del equipo de curación,
quiero editar los datos de una agencia existente,
para corregir o actualizar su información.

**Acceptance Criteria:**

**Given** una agencia existente
**When** se edita su nombre o ubicación
**Then** los cambios se guardan y se reflejan de inmediato en cualquier lectura posterior

### Story 2.3: Alta de vehículo (con marca)

Como miembro del equipo de curación,
quiero dar de alta un vehículo asociado a una agencia y marca, con año, precio, carrocería, transmisión/motor, equipamiento y color,
para que exista contenido real que los compradores puedan descubrir y filtrar.

**Acceptance Criteria:**

**Given** una agencia existente
**When** se da de alta un vehículo con marca, año, precio, carrocería, transmisión/motor, equipamiento y color
**Then** se guarda con esos campos estructurados (base de FR-4 a FR-9)
**And** el vehículo queda `is_active = true` con `last_updated` fijado por el servidor al momento de creación (AD-9)

### Story 2.4: Editar vehículo

Como miembro del equipo de curación,
quiero editar los atributos o el precio de un vehículo existente,
para mantenerlo al día en la actualización semanal.

**Acceptance Criteria:**

**Given** un vehículo existente
**When** se edita cualquiera de sus atributos
**Then** los cambios se guardan y `last_updated` se refresca automáticamente en el servidor (AD-9)

### Story 2.5: Desactivar vehículo (retirar del catálogo)

Como miembro del equipo de curación,
quiero desactivar un vehículo sin borrarlo,
para retirarlo de la búsqueda sin romper comparaciones ya guardadas que lo referencian.

**Acceptance Criteria:**

**Given** un vehículo activo
**When** se desactiva
**Then** `is_active` pasa a `false`, deja de aparecer en listados/búsqueda, y la fila nunca se borra de la base de datos (convención de catálogo)
**And** cualquier `SavedComparison` que ya lo referenciaba conserva su snapshot congelado sin verse afectada (AD-10)

## Epic 3: Descubrimiento de vehículos

Un comprador ubica agencias por ubicación, ve qué marcas manejan, lista sus vehículos y los filtra por año, precio, carrocería, transmisión, equipamiento, color y disponibilidad. Relacionado: FR-1 a FR-9, NFR-2, AD-3, AD-6, AD-9, AD-12, AD-13, UX-DR1/3/4/12/13/14/16/17.

**Criterio transversal (aplica a todas las historias de esta épica):** un solo `button-primary` visible por pantalla (UX-DR10), microcopy de marca verbatim de `EXPERIENCE.md` (UX-DR15), y piso de accesibilidad WCAG 2.1 AA — contraste verificado, targets táctiles ≥44×44px, foco visible con `{colors.accent}`, semántica adecuada por componente (UX-DR16).

### Story 3.1: Tokens de marca en Angular

Como comprador,
quiero que la app tenga la identidad visual correcta (clara/oscura) desde la primera pantalla,
para que se sienta consistente y confiable.

**Acceptance Criteria:**

**Given** la app cargada
**When** se activa modo oscuro
**Then** los colores cambian según los tokens de `DESIGN.md` sin inconsistencias (AD-3)

### Story 3.2: Ubicar agencias y ver sus marcas

Como comprador,
quiero ubicar agencias por ciudad/estado/país y ver qué marcas maneja cada una,
para saber dónde buscar.

**Acceptance Criteria:**

**Given** agencias con al menos 1 vehículo activo en Monterrey
**When** el comprador abre la búsqueda
**Then** ve la lista de agencias con las marcas que manejan (derivado, AD-13)
**And** una agencia sin vehículos activos no aparece en ningún listado

### Story 3.3: Listar vehículos por agencia

Como comprador,
quiero ver los vehículos disponibles de una agencia,
para empezar a explorar opciones.

**Acceptance Criteria:**

**Given** una agencia con vehículos activos
**When** el comprador ve resultados
**Then** aparecen tarjetas de vehículo con nombre, precio "aprox.", agencia y disponibilidad estimada (AD-9)
**And** mientras carga se muestra un skeleton, no una pantalla en blanco

### Story 3.4: Filtrar por año y precio

Como comprador,
quiero filtrar vehículos por año y precio,
para acotar mis opciones.

**Acceptance Criteria:**

**Given** resultados cargados
**When** aplico un filtro de año o precio
**Then** los resultados se recalculan sin botón "Aplicar", en menos de 2 segundos (NFR-2, AD-12)

### Story 3.5: Filtrar por carrocería, transmisión/motor y equipamiento

Como comprador,
quiero filtrar por carrocería, transmisión/motor y equipamiento,
para refinar más mi búsqueda.

**Acceptance Criteria:**

**Given** resultados cargados
**When** combino estos filtros con año/precio
**Then** los resultados reflejan todos los filtros activos simultáneamente

### Story 3.6: Filtrar por color/disponibilidad y panel "Todos los filtros"

Como comprador,
quiero filtrar por color y disponibilidad estimada, y ver todos mis filtros activos en un solo panel,
para no perder de vista qué apliqué entre 7 dimensiones.

**Acceptance Criteria:**

**Given** los 7 filtros disponibles
**When** abro "Todos los filtros"
**Then** veo un panel con todo lo activo y puedo limpiarlo de una vista

### Story 3.7: Detalle de vehículo

Como comprador,
quiero ver la ficha completa de un vehículo,
para conocer todas sus características antes de decidir.

**Acceptance Criteria:**

**Given** una tarjeta de vehículo
**When** toco la fila (no el botón de comparar)
**Then** navego a Detalle de vehículo con specs completas, agencia y disponibilidad estimada

### Story 3.8: Perfil de agencia

Como comprador,
quiero ver el perfil de una agencia con su catálogo completo,
para explorar todas sus opciones de una vez.

**Acceptance Criteria:**

**Given** el nombre de una agencia visible
**When** lo toco
**Then** navego a su Perfil con las marcas que maneja y su catálogo completo

### Story 3.9: Estado sin resultados

Como comprador,
quiero saber claramente cuando mis filtros no arrojan nada,
para poder ajustarlos.

**Acceptance Criteria:**

**Given** filtros que no encuentran coincidencias
**When** se aplican
**Then** se muestra el panel de estado vacío con el CTA "Ajustar filtros"

## Epic 4: Comparación de vehículos

Un comprador selecciona hasta 3 vehículos y los ve comparados lado a lado (o apilados en móvil) para decidir con más información. Relacionado: FR-10, AD-8, UX-DR5/6/7/8/16/17.

**Criterio transversal (aplica a todas las historias de esta épica):** un solo `button-primary` visible por pantalla (UX-DR10), microcopy de marca verbatim de `EXPERIENCE.md` (UX-DR15), y piso de accesibilidad WCAG 2.1 AA — incluye encabezados semánticos (`scope="col"`/`scope="row"`) en `comparison-table` y anuncio completo de `availability-badge` a lector de pantalla (UX-DR16).

### Story 4.1: Agregar/quitar un vehículo de la comparación

Como comprador,
quiero agregar o quitar un vehículo de mi selección para comparar,
para armar el grupo que quiero evaluar.

**Acceptance Criteria:**

**Given** una tarjeta o el detalle de un vehículo
**When** toco "Agregar a comparar"
**Then** se agrega a la selección activa y el botón cambia a "En comparación ✓"
**And** tocarlo de nuevo lo quita de la selección

### Story 4.2: Barra de comparación con conteo

Como comprador,
quiero ver cuántos vehículos llevo seleccionados y poder avanzar a comparar,
para saber en qué punto estoy.

**Acceptance Criteria:**

**Given** al menos 1 vehículo seleccionado
**When** navego por resultados
**Then** veo una barra fija con el conteo y nombres seleccionados
**And** el CTA "Comparar" solo se habilita con 2 o más seleccionados

### Story 4.3: Tope de 3 vehículos con bloqueo explícito

Como comprador,
quiero que el sistema me impida seleccionar más de 3 vehículos,
para que la comparación se mantenga legible.

**Acceptance Criteria:**

**Given** 3 vehículos ya seleccionados
**When** intento agregar un cuarto
**Then** el botón de esa tarjeta se deshabilita con el caption "Máximo 3 — quita uno primero"
**And** cualquier intento de forzarlo directo contra la API es rechazado por el trigger de Postgres (AD-8), no solo por la UI

### Story 4.4: Ver tabla comparativa (escritorio)

Como comprador,
quiero ver mis vehículos seleccionados comparados lado a lado,
para decidir con la información junta.

**Acceptance Criteria:**

**Given** 2 o 3 vehículos seleccionados
**When** toco "Comparar"
**Then** veo la tabla con precio/año, carrocería, transmisión/motor, equipamiento y disponibilidad estimada de cada uno
**And** cada columna tiene su propia acción para quitar ese vehículo

### Story 4.5: Ver comparación en móvil (tarjetas apiladas)

Como comprador en mi teléfono,
quiero ver la misma comparación sin tener que hacer scroll horizontal incómodo,
para leerla de un tirón.

**Acceptance Criteria:**

**Given** la misma comparación en viewport móvil
**When** se muestra
**Then** aparece como tarjetas apiladas por vehículo (Propuesta B elegida), no como tabla con scroll horizontal

### Story 4.6: Quitar un vehículo desde la comparación

Como comprador,
quiero quitar un vehículo directamente desde la vista de comparación,
para ajustarla sin volver a resultados.

**Acceptance Criteria:**

**Given** la tabla o tarjetas de comparación
**When** quito un vehículo
**Then** la comparación se recalcula en el momento sin salir de la pantalla

## Epic 5: Cuenta, guardar y compartir

Un comprador crea una cuenta simple, guarda una comparación para consultarla después, y comparte un link público de solo lectura — sin fricción de cuenta salvo al momento de guardar. Relacionado: FR-11, FR-12, FR-15, FR-16, AD-4, AD-5, AD-7, AD-14, AD-15, UX-DR9/12/13/18.

**Criterio transversal (aplica a todas las historias de esta épica):** un solo `button-primary` visible por pantalla (UX-DR10), microcopy de marca verbatim de `EXPERIENCE.md` (UX-DR15), y piso de accesibilidad WCAG 2.1 AA — validación inline por campo, foco visible, `Esc` cierra el modal de cuenta (UX-DR16).

### Story 5.1: Crear cuenta con email

Como comprador,
quiero crear una cuenta con mi email y un código de un solo uso,
para poder guardar comparaciones.

**Acceptance Criteria:**

**Given** un email válido
**When** solicito el código y lo confirmo
**Then** se crea mi cuenta y recibo una sesión válida (JWT almacenado de forma segura, AD-4)

### Story 5.2: Iniciar sesión con email

Como comprador con cuenta existente,
quiero iniciar sesión con un código de un solo uso,
para acceder a mis comparaciones guardadas.

**Acceptance Criteria:**

**Given** una cuenta existente
**When** solicito el código y lo confirmo
**Then** obtengo una sesión válida y accedo a "Mis comparaciones guardadas"

### Story 5.3: Guardar una comparación (cuenta al vuelo)

Como comprador,
quiero guardar una comparación que ya armé, creando cuenta en el momento si no tengo una,
para no perder el trabajo que ya hice.

**Acceptance Criteria:**

**Given** una comparación armada sin sesión
**When** toco "Guardar"
**Then** se abre el gate de cuenta sobre la tabla, y al completarla la comparación ya armada queda guardada automáticamente, sin rehacer la selección (AD-15)
**And** con sesión activa, "Guardar" persiste directo con confirmación inline "Guardada.", sin modal

### Story 5.4: Ver mis comparaciones guardadas

Como comprador con cuenta,
quiero ver la lista de mis comparaciones guardadas,
para retomarlas cuando las necesite.

**Acceptance Criteria:**

**Given** una sesión con comparaciones guardadas
**When** abro "Mis comparaciones guardadas"
**Then** veo la lista con fecha de guardado y datos congelados a ese momento (AD-10)
**And** si no tengo ninguna, veo el estado vacío con el CTA "Buscar vehículos"

### Story 5.5: Compartir una comparación

Como comprador,
quiero compartir un link público de mi comparación,
para que alguien más la vea sin necesitar cuenta.

**Acceptance Criteria:**

**Given** una comparación armada (con o sin sesión)
**When** toco "Compartir"
**Then** se genera un link público de solo lectura con token no secuencial (AD-7) y se copia al portapapeles
**And** una vez compartida, quitar un vehículo no modifica esa misma fila — se bloquea o genera una nueva comparación (AD-14)

### Story 5.6: Ver una comparación compartida sin cuenta

Como alguien que recibe un link compartido,
quiero ver la comparación sin necesitar cuenta,
para formar mi propia opinión.

**Acceptance Criteria:**

**Given** un link compartido válido
**When** lo abro sin sesión
**Then** veo la misma tabla/tarjetas en solo lectura, sin acción de quitar, con el CTA "Buscar tus propios vehículos"
**And** si ya no es válido (comparación borrada o vehículo fuera del catálogo), veo "Esta comparación ya no está disponible." sin gate de cuenta

## Epic 6: Paridad móvil (Flutter)

Todo lo construido en las épicas 2 a 5 queda disponible también en la app móvil, con el mismo comportamiento e identidad visual — como codebase independiente (AD-2), no un port automático. Relacionado: FR-13, NFR-3, AD-2, AD-3, UX-DR2/17.

**Criterio transversal (aplica a todas las historias de esta épica):** un solo botón primario visible por pantalla (UX-DR10), microcopy de marca verbatim de `EXPERIENCE.md` (UX-DR15), y piso de accesibilidad WCAG 2.1 AA equivalente al de la web, verificado de forma nativa en Flutter (UX-DR16).

### Story 6.1: Esqueleto Flutter con tokens de marca

Como comprador en mi teléfono,
quiero que la app tenga la misma identidad visual que la web,
para reconocer la marca sin importar el dispositivo.

**Acceptance Criteria:**

**Given** la app Flutter escaleada y consumiendo la spec OpenAPI (AD-16)
**When** se activa modo oscuro
**Then** los colores coinciden con los tokens de `DESIGN.md`, verificados independientemente contra el documento, no contra la implementación de Angular (AD-2, AD-3)

### Story 6.2: Descubrimiento en móvil

Como comprador en mi teléfono,
quiero ubicar agencias, ver sus marcas, listar vehículos y filtrarlos por las 7 dimensiones,
para buscar igual que en la web.

**Acceptance Criteria:**

**Given** el catálogo ya poblado (Épica 2)
**When** busco y filtro en Flutter
**Then** el comportamiento coincide con la web: mismos filtros, mismo estado vacío, mismo Detalle de vehículo y Perfil de agencia (NFR-3)

### Story 6.3: Comparación en móvil

Como comprador en mi teléfono,
quiero seleccionar hasta 3 vehículos y verlos comparados en tarjetas apiladas,
para decidir igual que en la web.

**Acceptance Criteria:**

**Given** vehículos seleccionados en Flutter
**When** toco "Comparar"
**Then** veo la comparación en tarjetas apiladas (el layout móvil ya elegido aplica de forma nativa aquí) con el mismo tope de 3 (AD-8)

### Story 6.4: Cuenta, guardar y compartir en móvil

Como comprador en mi teléfono,
quiero crear cuenta, guardar y compartir comparaciones,
para tener la misma funcionalidad completa que en la web.

**Acceptance Criteria:**

**Given** una comparación armada en Flutter
**When** creo cuenta/inicio sesión, guardo o comparto
**Then** el comportamiento (OTP por email, gate al guardar, link público que nunca expira) coincide exactamente con la web, contra el mismo contrato de API (AD-4, AD-15, AD-7)

### Story 6.5: Verificación de paridad funcional cruzada

Como comprador que alterna entre web y móvil,
quiero que ambas plataformas se comporten igual,
para no encontrar sorpresas al cambiar de dispositivo.

**Acceptance Criteria:**

**Given** las historias 6.1 a 6.4 completas
**When** se compara cada flujo (buscar, comparar, guardar, compartir) entre Angular y Flutter
**Then** ambos cumplen el mismo contrato OpenAPI (AD-16) y el mismo comportamiento — verificados cada uno contra la spec y `EXPERIENCE.md`, nunca uno contra el otro (AD-2, NFR-3)
