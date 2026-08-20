---
name: '[Proyecto Auto]'
status: final
sources:
  - '{planning_artifacts}/prds/prd-auto-2026-08-19/prd.md'
updated: 2026-08-19
---

# EXPERIENCE.md — [Proyecto Auto]

> Nombre de trabajo, branding definitivo pendiente. Superficie web + app móvil con paridad funcional (FR-13). Emparejado con `DESIGN.md`. Ambos spines ganan sobre cualquier mock/wireframe en caso de conflicto.

## Foundation

Producto multi-superficie: **web responsiva** + **app móvil**, con paridad funcional obligatoria (FR-13, NFR-3). Stack de implementación: **Angular** (última versión) con **PrimeNG** como UI kit — Table para la comparación, Drawer/Dialog para filtros y el gate de cuenta, componentes de formulario para el alta de cuenta. `DESIGN.md` es la referencia de identidad visual e inyecta sus tokens en un `definePreset` de PrimeNG (Aura) con `darkModeSelector` de clase; este documento es la capa de comportamiento.

`[ASSUMPTION]` **Forma de la app móvil no resuelta aquí.** El PRD confirma "app móvil" como superficie (alcance MVP, FR-13) pero no si se implementa como nativa/híbrida o como web responsiva/PWA. Este spine trata "móvil" como un segundo target de viewport (angosto) que ambos flujos y patrones deben cubrir, igual que "escritorio" (ancho) — la decisión de empaquetado (Capacitor, PWA, nativo puro) es de arquitectura, no de UX, igual que el PRD marcó NFR-5 para `bmad-architecture`.

**Modelo de cuenta.** Buscar, filtrar y comparar es completamente anónimo — no hay gate de ningún tipo hasta que el usuario intenta **guardar** una comparación (FR-11), momento en que se exige cuenta simple por email o teléfono (FR-15). Compartir (FR-12) genera un link público de solo lectura, visible sin cuenta para quien lo recibe. Ninguna otra acción del producto requiere sesión.

**Frescura de datos.** El catálogo se actualiza semanalmente (NFR-1); "disponibilidad" nunca se presenta como tiempo real — siempre lleva la etiqueta "estimada" en la UI (FR-9), vía el componente `availability-badge` de `DESIGN.md`, nunca solo como nota a pie.

**Tope de comparación.** Máximo 3 vehículos simultáneos (decisión confirmada, ver `.memlog.md`).

## Information Architecture

| Superficie | Se llega desde | Propósito |
|---|---|---|
| Búsqueda/Resultados | Apertura de la app; resultado de ajustar filtros | Listar y filtrar vehículos disponibles en Monterrey |
| Detalle de vehículo | Tap/click en una `vehicle-result-card` | Ficha completa de una versión: specs, agencia, disponibilidad estimada |
| Perfil de agencia | Nombre de agencia en tarjeta o detalle | Qué marcas comercializa esa agencia y su catálogo completo |
| Comparación (tabla) | Botón "Comparar" desde `compare-footer-bar` (2–3 seleccionados) | Tabla lado a lado: precio/año, carrocería, transmisión/motor, equipamiento |
| Crear cuenta / Iniciar sesión | Intento de "Guardar" sin sesión; enlace "Iniciar sesión" | Gate mínimo (email o teléfono) exigido solo para guardar |
| Mis comparaciones guardadas | Menú de cuenta, tras iniciar sesión | Historial de comparaciones guardadas por el usuario |
| Comparación compartida (vista pública) | Link compartido, sin cuenta | Misma tabla de comparación en solo lectura, sin gate |
| *Estado* — sin resultados de búsqueda | Búsqueda/Resultados, tras filtrar | Ninguna coincidencia con los filtros activos |
| *Estado* — sin comparaciones guardadas | Mis comparaciones guardadas, usuario nuevo | Cuenta recién creada, historial vacío |

Sin navegación por drawer/hamburguesa como patrón por defecto: la barra de filtros y la `compare-footer-bar` son los elementos persistentes de cada superficie de listado. Modal/drawer se apilan un solo nivel (filtro abierto, o gate de cuenta abierto — nunca ambos a la vez).

→ Referencia de composición: [`mockups/direction-premium.html`](mockups/direction-premium.html) (Búsqueda/Resultados escritorio + móvil, Comparación, estado vacío), [`mockups/color-themes-premium.html`](mockups/color-themes-premium.html) (paleta aplicada al mismo fragmento). Los spines ganan sobre estos mocks en caso de conflicto.

## Voice and Tone

Microcopy. La voz de marca y la postura estética viven en `DESIGN.md` → Brand & Style.

| Do | Don't |
|---|---|
| "Precio aproximado — catálogo actualizado semanalmente." | "¡Precio garantizado!" |
| "Disponibilidad estimada" | "¡En stock ahora! 🔥" |
| "Aún no hay coincidencias. Ajusta el rango de precio o el año para ver más opciones." | "¡Ups! No encontramos nada 😢 Intenta de nuevo" |
| "2 seleccionados — Nissan Versa · VW Virtus" | "¡2 autos geniales en tu comparación!" |
| "Crea una cuenta para guardar esta comparación." | "¡Regístrate ya y no pierdas nada!" |
| "No pudimos guardar. Tu comparación sigue aquí — intenta de nuevo." | "Error 500: fallo en el servidor" |
| Frases cortas y completas, sin signos de exclamación, sin emojis. | Tono de urgencia o entusiasmo artificial en cualquier microcopy. |

La marca le habla a Laura como a alguien que ya sabe comparar opciones — informativa, nunca vendedora.

## Component Patterns

Comportamiento. Las especificaciones visuales viven en `DESIGN.md` → Components.

| Componente | Uso | Reglas de comportamiento |
|---|---|---|
| `vehicle-result-card` | Búsqueda/Resultados, Perfil de agencia | Tap/click en cualquier parte de la fila (excepto el botón de comparar) abre Detalle de vehículo. El botón de comparar es la única zona con su propio target de tap independiente. |
| `filter-chip` | Búsqueda/Resultados (fila persistente) | Tap abre un selector acotado a esa dimensión de filtro (popover en escritorio, hoja inferior en móvil — ver Interaction Primitives). El chip refleja el valor activo una vez aplicado; "Limpiar" dentro del selector vuelve el chip a su estado inactivo. |
| `compare-select-button` | `vehicle-result-card`, Detalle de vehículo | Toggle: agrega/quita ese vehículo de la selección activa. Al llegar a 3 seleccionados, el botón se deshabilita en toda tarjeta no seleccionada con caption "Máximo 3 — quita uno primero"; tarjetas ya seleccionadas conservan su botón activo para poder quitarse. |
| `compare-footer-bar` | Búsqueda/Resultados, Detalle de vehículo (cuando hay ≥1 seleccionado) | Aparece solo con ≥1 vehículo seleccionado; se oculta con 0. Muestra el conteo y los nombres; el CTA "Comparar" solo se habilita con ≥2 seleccionados (FR-10 exige "dos o más"). |
| `comparison-table` | Comparación (tabla), Comparación compartida | **Escritorio:** hasta 3 columnas de vehículo + 1 de etiqueta, grilla única. **Móvil:** tarjetas apiladas verticalmente, una por vehículo, cada una con su propio bloque de atributos (ver Responsive & Platform) — incluye la acción de quitar como un ícono en el encabezado de cada tarjeta, misma regla que en escritorio. En ambas superficies, la comparación se recalcula al quitar un vehículo sin salir de la pantalla. En la vista compartida (sin cuenta) esta acción de quitar no existe — la tabla/tarjetas son de solo lectura. |
| `availability-badge` | `vehicle-result-card`, Detalle de vehículo, `comparison-table` | Siempre acompañado del microcopy "estimada" o una fecha relativa de última actualización ("actualizado hace 3 días"). Nunca aparece como único indicador de stock sin ese calificador. |
| `account-form-field` | Crear cuenta / Iniciar sesión | Un solo campo (email o teléfono) más confirmación — sin contraseña compleja en el MVP (cuenta "simple", FR-15). Validación inline al perder foco, nunca solo al enviar. |
| `button-primary` / `button-secondary` | Global | Máximo un `button-primary` visible por pantalla — es la acción que el flujo quiere que el usuario tome. Todo lo demás es `button-secondary`. |
| `empty-state-panel` | Estado "sin resultados", estado "sin comparaciones guardadas" | Un único CTA `button-secondary` por panel: "Ajustar filtros" en sin-resultados, "Buscar vehículos" en sin-comparaciones-guardadas. |

## State Patterns

| Superficie | Vacío | Carga fría | Error | Frescura de dato |
|---|---|---|---|---|
| Búsqueda/Resultados | `empty-state-panel`: "Aún no hay coincidencias." + ajustar filtros | Skeleton de 3–4 `vehicle-result-card` con la misma altura que el contenido real | Banner no bloqueante: "No pudimos cargar resultados. Reintentar." — filtros activos se conservan | Cada tarjeta con precio/`availability-badge` porta "aprox." / "estimada" |
| Detalle de vehículo | N/A (siempre llega desde un resultado existente) | Skeleton de ficha (imagen placeholder + líneas de specs) | "No pudimos cargar este vehículo. Reintentar." con botón volver a Resultados | Precio y disponibilidad con el mismo calificador que en la tarjeta |
| Perfil de agencia | N/A — una agencia sin al menos 1 vehículo cargado no aparece en ningún listado (decisión confirmada), por lo que este estado nunca es alcanzable | Skeleton de encabezado de agencia + lista de vehículos | "No pudimos cargar esta agencia. Reintentar." | Catálogo de la agencia hereda el mismo aviso de actualización semanal |
| Comparación (tabla) | N/A (solo se llega con ≥2 seleccionados) | No aplica — la tabla se construye desde datos ya cargados en Resultados | "No pudimos generar la comparación. Reintentar." conservando la selección | Fila dedicada de disponibilidad estimada por vehículo |
| Crear cuenta / Iniciar sesión | N/A | N/A | Inline por campo: "Este correo no es válido." / "No pudimos crear la cuenta. Intenta de nuevo." | N/A |
| Mis comparaciones guardadas | `empty-state-panel`: "Aún no guardas comparaciones." + buscar vehículos | Skeleton de 2–3 filas de comparación guardada | "No pudimos cargar tus comparaciones. Reintentar." | Cada comparación guardada muestra la fecha en que se guardó (no se auto-actualiza con precios nuevos — dato congelado al momento de guardar, confirmado) |
| Comparación compartida (pública) | Link inválido (comparación borrada por su dueño, o el vehículo referenciado salió del catálogo): "Esta comparación ya no está disponible." sin gate de cuenta. El link en sí **nunca expira por tiempo** (decisión confirmada) — solo deja de resolver si el contenido detrás ya no existe. | Skeleton igual al de Comparación (tabla) | "No pudimos cargar esta comparación compartida. Reintentar." | Misma fila de disponibilidad estimada; sin acción de refrescar (vista de solo lectura) |
| *Estado* — sin resultados de búsqueda | Es el propio estado — ver Búsqueda/Resultados arriba | — | — | — |
| *Estado* — sin comparaciones guardadas | Es el propio estado — ver Mis comparaciones guardadas arriba | — | — | — |

## Interaction Primitives

**Filtros (patrón chip + selector acotado + drawer "todos los filtros", confirmado).** El set de filtros es denso: año, precio, carrocería, transmisión/motor, equipamiento/versión, color y disponibilidad estimada (7 dimensiones, FR-4 a FR-9). La fila de `filter-chip` queda siempre visible con scroll horizontal propio en ambas superficies (fiel a `mockups/direction-premium.html`). Tocar un chip abre un selector acotado a esa sola dimensión — popover anclado en escritorio, hoja inferior (bottom sheet) en móvil — nunca un formulario completo de una sola vez. Un chip adicional fijo, "Todos los filtros", abre un drawer/panel completo para revisar y limpiar todo lo activo de una sola vista — necesario porque con 7 dimensiones un usuario puede perder de vista qué tiene aplicado solo mirando la fila de chips. Resultados se recalculan en cuanto se cierra cualquier selector, sin botón "Aplicar" adicional (cumple NFR-2, <2s).

**Selección de comparación (tope de 3).** `compare-select-button` agrega/quita un vehículo del set activo desde cualquier `vehicle-result-card` o Detalle de vehículo. La `compare-footer-bar` refleja el conteo en vivo. Al llegar a 3/3: los botones de tarjetas no seleccionadas se deshabilitan (no se reemplaza automáticamente al 4to intento — el usuario debe quitar uno explícitamente desde una tarjeta ya seleccionada o desde la propia `compare-footer-bar`, que lista los nombres y permite quitar ahí mismo). Se prefiere este bloqueo explícito sobre un reemplazo silencioso (confirmado) porque una comparación de auto es una decisión deliberada — no queremos que Laura pierda un vehículo de su selección sin darse cuenta.

**Guardar.** "Guardar comparación" desde la tabla de Comparación. Sin sesión activa: abre el gate de Crear cuenta/Iniciar sesión como modal sobre la tabla (no navega fuera); al completar la cuenta, la comparación que estaba en pantalla se guarda automáticamente — Laura no repite el paso. Con sesión activa: guarda directo, confirmación inline breve ("Guardada.") sin modal.

**Compartir.** "Compartir" desde la tabla de Comparación (disponible con o sin sesión — no requiere guardar primero). Genera un link público de solo lectura y lo copia al portapapeles con confirmación inline ("Link copiado."). El link no expone ninguna acción de edición ni de guardado a quien lo abre sin cuenta.

## Accessibility Floor

**WCAG 2.1 AA como piso** (confirmado; sin requerimiento explícito de accesibilidad en el PRD, se adopta como baseline por defecto). Contraste visual detallado en `DESIGN.md`; aquí solo el comportamiento y las implicaciones concretas de la paleta elegida.

**Contraste, pares reales de la paleta (verificado, no solo declarado):**
- `{colors.ink}` sobre `{colors.background}` (claro): **16.2:1**
- `{colors.ink-dark}` sobre `{colors.background-dark}` (oscuro): **16.3:1**
- `{colors.ink-secondary}` sobre `{colors.background}` (claro): **5.4:1** — pasa AA texto normal
- `{colors.ink-secondary-dark}` sobre `{colors.background-dark}` (oscuro): **6.5:1**
- `{colors.accent}` como texto sobre `{colors.background}` (eyebrow, precio): **6.7:1**
- `{colors.accent-foreground}` sobre `{colors.accent}` (botón primario, claro): **7.4:1**
- `{colors.accent-foreground-dark}` sobre `{colors.accent-dark}` (botón primario, oscuro): **9.8:1**
- `{colors.success}` sobre `{colors.surface}` (badge de disponibilidad, claro): **4.8:1** — pasa AA texto normal pero al límite; `availability-badge` debe usarse siempre en `{typography.caption}` con trazo/borde, nunca en texto más chico ni sobre `{colors.background}` directamente sin verificar de nuevo
- `{colors.success-dark}` sobre `{colors.surface-dark}` (oscuro): **6.2:1**

Ninguna combinación de texto de la paleta cae bajo AA; el par más ajustado (`success` claro, 4.8:1) queda anotado arriba para que implementación no lo debilite con opacidad o un tamaño menor.

**Comportamiento:**
- Todo control interactivo (chip, badge, botón, fila de tarjeta) con target táctil ≥44×44px en móvil.
- Foco visible en todo input y botón usando `{colors.accent}` como anillo — nunca solo un cambio de color de fondo.
- `comparison-table` con encabezados de columna semánticos (`scope="col"`) y de fila (`scope="row"` en la columna de etiqueta) para que un lector de pantalla anuncie "Precio y año, Nissan Versa: $389,000, 2025" en vez de una celda suelta.
- `availability-badge` anuncia el calificador completo a lector de pantalla ("Disponibilidad estimada: disponible"), nunca solo un ícono o color.
- Orden de tabulación sigue el orden de lectura en cada superficie; `Esc` cierra el selector de filtro o el modal de cuenta abiertos.
- Modo oscuro no es solo estético: es requisito duro (ver `DESIGN.md`) y debe pasar los mismos contrastes verificados arriba en cualquier punto de la app, no solo en las superficies de marketing.

## Responsive & Platform

| Breakpoint | Comportamiento |
|---|---|
| Escritorio (≥ ancho de `.browser` en el mock, ~620px de contenido) | `filter-chip` en fila con divisores verticales finos (no píldora). `compare-footer-bar` vive al final de la lista de resultados, no fija. `comparison-table` se muestra completa sin scroll horizontal hasta 3 columnas de vehículo. |
| Móvil (ancho de `.phone-shell` en el mock, ~320–390px) | `filter-chip` en píldora (`{rounded.full}`), fila con scroll horizontal. `compare-footer-bar` fija al fondo de la pantalla sobre el contenido con scroll. `comparison-table` se muestra como una tarjeta completa apilada por vehículo (nombre, agencia, precio y la lista de atributos en filas verticales dentro de la tarjeta), no como la grilla con scroll horizontal de escritorio — decisión confirmada tras comparar 4 propuestas de layout (ver `.memlog.md`); prioriza leer cada vehículo completo sin scroll horizontal, a costa de exigir memoria para comparar la misma fila entre tarjetas. |

Ambas superficies exponen el mismo set de funcionalidades del MVP (NFR-3) — ningún flujo de los tres anteriores tiene un paso exclusivo de una sola superficie.

## Inspiration & Anti-patterns

- **Explorado y descartado — "Fresco y directo" (tipo Rappi):** color saturado, formas muy redondeadas, microcopy casual y directo pensado para pedir comida con el pulgar. Se descartó porque una compra de auto es una decisión de meses, no un antojo de 20 minutos — ese registro le resta credibilidad al dato de precio/disponibilidad justo cuando el usuario más necesita confiar en él. → [`.working/direction-fresco-directo.html`](.working/direction-fresco-directo.html) (no elegida — referencia de lo descartado, no de lo construido).
- **Explorado y descartado — "Confiable y sobrio" (fintech):** azules y neutros, cero ornamento, jerarquía tipográfica estricta, tomado del lenguaje visual de una app bancaria seria. El objetivo — que el precio se sienta tan confiable como un saldo bancario — era correcto, pero el resultado leía genérico, sin personalidad de marca propia. La paleta "Lujo frío/tech" finalmente elegida hereda ese mismo registro azul/frío de credibilidad, pero montado sobre la estructura editorial de Premium en vez de sobre un layout de app bancaria — se queda con lo que funcionaba de "confiable/sobrio" sin heredar su genericidad. → [`.working/direction-confiable-sobrio.html`](.working/direction-confiable-sobrio.html) (no elegida).
- **Explorado y no elegido — híbrido premium/fresco:** una cuarta dirección que mezclaba estructura Premium con acentos del registro fresco/directo; se descartó a favor de Premium puro una vez que la paleta "Lujo frío/tech" resolvió por sí sola la calidez que el híbrido buscaba aportar. → [`.working/direction-hibrido-premium-fresco.html`](.working/direction-hibrido-premium-fresco.html) (no elegida).
- **Lifted — configurador de auto de lujo:** la referencia editorial (serif para el nombre/precio del vehículo, mucho aire, separadores finos en vez de tarjetas con sombra) viene deliberadamente del registro de un configurador de vehículo premium — cada vehículo se siente curado, no listado.
- **Rejected — badges de urgencia ("¡Solo queda 1!", contadores regresivos):** el dato de disponibilidad es semanal y estimado (NFR-1, FR-9); fabricar urgencia sobre un dato que puede tener días de desfase mina directamente la contra-métrica de discrepancia reportada del PRD.
- **Rejected — gamificación de guardado/compartido (streaks, insignias):** guardar y compartir son señales de confianza en el dato (ver Métricas de éxito del PRD), no hábitos a reforzar con mecánicas de juego.

## Key Flows

### Flujo 1 — UJ-1: Laura busca y compara antes de decidir

**Protagonista:** Laura, quiere cambiar de auto.

1. Laura abre la app (web o móvil) y busca vehículos, filtrando por agencias presentes en su estado/municipio o directamente por precio.
2. Búsqueda/Resultados carga; usa la fila de `filter-chip` para acotar por precio y carrocería. Los resultados se recalculan al cerrar cada selector, sin esperar un botón "Aplicar".
3. Antes de decidir, toca el nombre de una agencia en una `vehicle-result-card` y abre Perfil de agencia — confirma qué otras marcas maneja esa agencia antes de seguir comparando, luego vuelve a Búsqueda/Resultados.
4. Revisa varias `vehicle-result-card` y agrega dos versiones a comparar con `compare-select-button`; la `compare-footer-bar` aparece con el conteo.
5. Toca "Comparar". La tabla de Comparación muestra precio y año, carrocería, transmisión/motor y equipamiento de las dos versiones lado a lado — cada precio y disponibilidad con su calificador de "aprox."/"estimada".
6. Decide agregar una tercera opción desde Detalle de vehículo antes de decidir; el tope de 3 se respeta sin fricción porque aún no lo alcanzó.
7. **Clímax:** Laura llega a la agencia ya informada — sabe qué quiere, conoce las versiones existentes, sus características y precios aproximados. Negocia con la tabla de comparación abierta en su teléfono, no con lo que el vendedor le diga primero.

Falla: la búsqueda/filtrado tarda o falla → banner no bloqueante, filtros activos se conservan, reintento no pierde el trabajo de Laura.

### Flujo 2 — Guardar comparación crea cuenta al vuelo (Diego, en la sala de su casa, antes de dormir)

1. Diego arma una comparación de 3 vehículos sin haber iniciado sesión — todo el flujo hasta aquí fue anónimo.
2. Toca "Guardar comparación" en la tabla.
3. Se abre el modal de Crear cuenta/Iniciar sesión sobre la tabla (no navega fuera de la pantalla). Ingresa su teléfono.
4. Confirma el código de un solo uso.
5. **Clímax:** el modal se cierra y la tabla que Diego ya armó aparece marcada como guardada — no tuvo que rehacer la selección después de crear la cuenta. La app recordó lo que estaba mirando.
6. Unos días después, Diego abre "Mis comparaciones guardadas" desde el menú de cuenta para retomar esa comparación antes de ir a la agencia — la encuentra con la fecha en que la guardó, datos congelados a ese momento.

Falla: el código no llega o expira → el modal permanece abierto con un botón "Reenviar código"; la comparación en pantalla no se pierde ni se descarta.

### Flujo 3 — Alguien sin cuenta abre un link compartido (Ana, cuñada de Laura, recibe el link por WhatsApp)

1. Laura le comparte a Ana el link público de su comparación de 2 vehículos.
2. Ana lo abre desde WhatsApp en su teléfono — no tiene cuenta en `[Proyecto Auto]` ni la necesita.
3. Ve la misma `comparison-table` en solo lectura: precio, año, carrocería, transmisión/motor y equipamiento de ambos vehículos, con el mismo aviso de disponibilidad estimada que vio Laura.
4. No hay acción de quitar vehículos ni de editar — solo un CTA secundario "Buscar tus propios vehículos" que la lleva a Búsqueda/Resultados anónima.
5. **Clímax:** Ana forma su propia opinión sobre qué vehículo prefiere sin tener que pedirle a Laura que le explique — el link mismo es la explicación. Si decide buscar por su cuenta, entra al mismo flujo anónimo que Laura usó en el Flujo 1, sin fricción de cuenta.

Falla: el link fue revocado o el vehículo ya no está en catálogo → estado "Esta comparación ya no está disponible." sin gate de cuenta, con el mismo CTA de buscar.
