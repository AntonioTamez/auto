---
name: '[Proyecto Auto]'
description: 'Plataforma de comparación de vehículos nuevos para Monterrey. Angular + PrimeNG (preset Aura); este DESIGN.md define los tokens de marca que se inyectan en PrimeNG vía definePreset — PrimeNG resuelve la mecánica de componentes, esta capa es la identidad visual.'
colors:
  background: '#F1F4F6'
  background-dark: '#0A0F16'
  surface: '#FFFFFF'
  surface-dark: '#121B26'
  ink: '#12181F'
  ink-dark: '#E7EDF3'
  ink-secondary: '#5A6570'
  ink-secondary-dark: '#8B97A5'
  border: '#DAE2E8'
  border-dark: '#22303F'
  accent: '#3E5872'
  accent-dark: '#A9BCCB'
  accent-foreground: '#FFFFFF'
  accent-foreground-dark: '#0A0F16'
  success: '#3D7F63'
  success-dark: '#5CA989'
typography:
  display:
    fontFamily: 'Georgia, "Times New Roman", serif'
    fontSize: '30px'
    fontWeight: '400'
    lineHeight: '1.25'
    letterSpacing: '0.01em'
  display-sm:
    fontFamily: 'Georgia, "Times New Roman", serif'
    fontSize: '21px'
    fontWeight: '400'
    lineHeight: '1.3'
  title:
    fontFamily: 'Georgia, "Times New Roman", serif'
    fontSize: '18px'
    fontWeight: '400'
    lineHeight: '1.3'
  title-sm:
    fontFamily: 'Georgia, "Times New Roman", serif'
    fontSize: '15px'
    fontWeight: '400'
    lineHeight: '1.3'
  price:
    fontFamily: 'Georgia, "Times New Roman", serif'
    fontSize: '19px'
    fontWeight: '400'
    lineHeight: '1.2'
  eyebrow:
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif'
    fontSize: '11px'
    fontWeight: '700'
    lineHeight: '1.4'
    letterSpacing: '0.16em'
  body:
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif'
    fontSize: '14px'
    fontWeight: '400'
    lineHeight: '1.6'
  caption:
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif'
    fontSize: '11.5px'
    fontWeight: '400'
    lineHeight: '1.4'
  label:
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif'
    fontSize: '11px'
    fontWeight: '700'
    lineHeight: '1.3'
    letterSpacing: '0.04em'
rounded:
  sm: '2px'
  DEFAULT: '4px'
  md: '6px'
  lg: '10px'
  xl: '30px'
  full: '9999px'
spacing:
  '1': '4px'
  '2': '8px'
  '3': '12px'
  '4': '16px'
  '5': '20px'
  '6': '24px'
  '7': '32px'
  '8': '44px'
  margin-mobile: '20px'
  margin-desktop: '44px'
  gutter: '24px'
components:
  vehicle-result-card:
    background: '{colors.surface}'
    background-dark: '{colors.surface-dark}'
    divider: '{colors.border}'
    divider-dark: '{colors.border-dark}'
    title-typography: '{typography.title}'
    price-color: '{colors.accent}'
    price-color-dark: '{colors.accent-dark}'
  filter-chip:
    background: 'transparent'
    background-active: '{colors.ink}'
    background-active-dark: '{colors.ink-dark}'
    foreground: '{colors.ink-secondary}'
    foreground-active: '{colors.surface}'
    foreground-active-dark: '{colors.surface-dark}'
    border: '{colors.border}'
    border-dark: '{colors.border-dark}'
    radius: '{rounded.full}'
  compare-select-button:
    background: 'transparent'
    border: '{colors.ink}'
    border-dark: '{colors.ink-dark}'
    foreground: '{colors.ink}'
    foreground-dark: '{colors.ink-dark}'
    background-selected: '{colors.ink}'
    background-selected-dark: '{colors.ink-dark}'
    foreground-selected: '{colors.surface}'
    foreground-selected-dark: '{colors.surface-dark}'
    radius: '{rounded.sm}'
  compare-footer-bar:
    background: '{colors.ink}'
    foreground: '{colors.ink-dark}'
    background-dark: '{colors.surface-dark}'
    foreground-dark: '{colors.ink-dark}'
    border-top-dark: '{colors.border-dark}'
    cta-background: '{colors.accent}'
    cta-background-dark: '{colors.accent-dark}'
    cta-foreground: '{colors.accent-foreground}'
    cta-foreground-dark: '{colors.accent-foreground-dark}'
    radius: '{rounded.sm}'
  comparison-table:
    header-border: '{colors.ink}'
    header-border-dark: '{colors.ink-dark}'
    row-divider: '{colors.border}'
    row-divider-dark: '{colors.border-dark}'
    label-column-color: '{colors.ink-secondary}'
    label-column-color-dark: '{colors.ink-secondary-dark}'
    vehicle-name-typography: '{typography.title}'
  availability-badge:
    foreground: '{colors.success}'
    foreground-dark: '{colors.success-dark}'
    border: '{colors.success}'
    border-dark: '{colors.success-dark}'
    background: 'transparent'
    typography: '{typography.caption}'
    radius: '{rounded.sm}'
  account-form-field:
    border: '{colors.border}'
    border-dark: '{colors.border-dark}'
    border-focus: '{colors.accent}'
    border-focus-dark: '{colors.accent-dark}'
    label-typography: '{typography.label}'
    radius: '{rounded.sm}'
  button-primary:
    background: '{colors.accent}'
    background-dark: '{colors.accent-dark}'
    foreground: '{colors.accent-foreground}'
    foreground-dark: '{colors.accent-foreground-dark}'
    typography: '{typography.label}'
    radius: '{rounded.sm}'
  button-secondary:
    background: 'transparent'
    border: '{colors.ink}'
    border-dark: '{colors.ink-dark}'
    foreground: '{colors.ink}'
    foreground-dark: '{colors.ink-dark}'
    typography: '{typography.label}'
    radius: '{rounded.sm}'
  empty-state-panel:
    background: '{colors.background}'
    background-dark: '{colors.background-dark}'
    accent-rule: '{colors.accent}'
    accent-rule-dark: '{colors.accent-dark}'
    heading-typography: '{typography.display-sm}'
    radius: '{rounded.lg}'
status: final
updated: 2026-08-19
---

# DESIGN.md — [Proyecto Auto]

> Nombre de trabajo, branding definitivo pendiente (ver Open Questions del PRD). `[Proyecto Auto]` se usa como placeholder literal en toda la marca hasta resolverse.

## Brand & Style

**[Proyecto Auto]** vende confianza informativa, no entusiasmo. Laura va a gastar el ahorro de meses en un vehículo; la marca tiene que sentirse como el criterio sereno de alguien que ya hizo la tarea por ella — no como una app de delivery, no como un banco genérico. La dirección **Premium** resuelve esto con una serif editorial para los momentos de decisión (nombre del vehículo, precio, encabezados), mucho aire entre elementos, separadores finos en vez de tarjetas con sombra, y botones de línea que piden ser leídos antes de tocados.

La paleta **"Lujo frío / tech"** — grafito, azul acero, platino — reemplaza la calidez cuero/bronce explorada inicialmente. El registro es el de un configurador de vehículo eléctrico premium: preciso, contemporáneo, ligeramente frío a propósito. Esa frialdad es la que le presta credibilidad al dato (precio, disponibilidad) sin caer en la asepsia bancaria de la dirección "confiable/sobrio" que también se exploró y se descartó (ver Inspiration & Anti-patterns en `EXPERIENCE.md`).

Composición y jerarquía de componentes se heredan de `direction-premium.html` sin desviación — layout, tipografía, densidad y personalidad quedaron confirmados como están. Los tokens de color de este documento son la única capa que cambia respecto a ese archivo.

→ Referencia visual: [`mockups/direction-premium.html`](mockups/direction-premium.html) — dirección elegida completa (layout, tipografía, densidad, componentes, escritorio + móvil, estado vacío). Este DESIGN.md gana sobre el mock en caso de conflicto.

## Colors

El sistema es deliberadamente contenido: dos superficies neutras, dos niveles de tinta, un acento cromático único y un verde reservado exclusivamente para señalizar disponibilidad. Todo token trae su par claro/oscuro (sufijo `-dark`); PrimeNG resuelve el cambio de esquema en runtime vía `darkModeSelector` basado en clase (no solo preferencia de SO — ver Components).

- **`background` (`#F1F4F6` claro / `#0A0F16` oscuro)** — lienzo general de la app. Gris-azulado casi imperceptible en claro; grafito casi negro en oscuro, no negro puro (evita el "agujero" de un OLED verdadero y deja que `surface` respire encima).
- **`surface` (`#FFFFFF` claro / `#121B26` oscuro)** — tarjetas, paneles, tabla de comparación, formularios. Distinción tonal mínima respecto a `background`, nunca por sombra (ver Elevation & Depth).
- **`ink` (`#12181F` claro / `#E7EDF3` oscuro)** — texto primario y, en modo claro, el relleno de las bandas de cromo "sólidas" (barra de comparación fija, encabezado de navegador/app) heredadas del mock — hace el mismo doble papel que el negro-marca original. En esas bandas, el texto usa `ink-dark` como color-sobre-oscuro incluso en modo claro (mismo patrón que "tinta clara sobre chrome oscuro").
- **`ink-secondary` (`#5A6570` claro / `#8B97A5` oscuro)** — texto de apoyo: nombre de agencia, subtítulos, "MXN aprox.".
- **`border` (`#DAE2E8` claro / `#22303F` oscuro)** — únicamente hairlines: separadores de tarjeta, filas de tabla, bordes de input. Nunca relleno.
- **`accent` — azul acero (`#3E5872` claro / `#A9BCCB` oscuro, platino)** — el color de marca. Precio, CTA primario ("Comparar", "Guardar"), eyebrows en mayúsculas, foco de inputs. `accent-foreground` (`#FFFFFF` claro / `#0A0F16` oscuro) es el texto que va sobre un relleno de acento.
- **`success` (`#3D7F63` claro / `#5CA989` oscuro)** — reservado en exclusiva para la etiqueta de disponibilidad estimada (badge "Disponible"). No se usa para nada más: ni confirmaciones genéricas, ni estados de formulario válido.

Evitar: gradientes, más de un color cromático activo a la vez, rojo/rosa para error (usar `ink` + microcopy claro, ver `EXPERIENCE.md`), y usar `success` fuera del badge de disponibilidad.

→ Referencia visual: [`mockups/color-themes-premium.html`](mockups/color-themes-premium.html), **Variación 2 — "Lujo frío / tech"** (claro y oscuro) — origen exacto de cada hex de esta sección.

## Typography

Dos familias, cada una con un trabajo fijo. **Georgia / "Times New Roman" (serif)** es la voz editorial: nombres de vehículo, precios, titulares de página, la etiqueta de cada versión en la tabla comparativa. **-apple-system / Segoe UI / Helvetica (sans, vía `.sans` en el mock)** es la voz de interfaz: eyebrows, chips de filtro, badges, botones, texto de apoyo. La serif nunca aparece en controles interactivos; la sans nunca aparece en el nombre de un vehículo o su precio.

| Rol | Uso |
|---|---|
| `{typography.display}` | Titular de página en escritorio (p. ej. "Vehículos disponibles") |
| `{typography.display-sm}` | Titular de página en móvil; encabezado de estado vacío |
| `{typography.title}` | Nombre de vehículo en tarjeta (escritorio) y en cabecera de tabla comparativa |
| `{typography.title-sm}` | Nombre de vehículo en tarjeta (móvil) |
| `{typography.price}` | Precio, siempre en `{colors.accent}` |
| `{typography.eyebrow}` | Etiqueta corta en mayúsculas sobre un titular ("Resultados en Monterrey"), siempre en `{colors.accent}` |
| `{typography.body}` | Descripciones, nombre de agencia, copy de apoyo |
| `{typography.caption}` | Badges, calificador de precio ("MXN aprox."), texto de chip |
| `{typography.label}` | Texto de botón, encabezado de columna de tabla, micro-etiquetas en mayúsculas |

No hay tamaños "display" heroicos más allá de `display` (30px) — el registro premium se construye con espacio y jerarquía tipográfica sobria, no con tamaños grandes.

## Layout & Spacing

Escala base de 4px (`{spacing.1}`…`{spacing.8}`: 4/8/12/16/20/24/32/44). El aire es la herramienta principal de jerarquía: cada tarjeta de resultado respira con `{spacing.6}`–`{spacing.7}` de padding vertical y un divisor de 1px (`{colors.border}`), nunca una tarjeta con sombra propia — eso es deliberado, ver Elevation & Depth.

Márgenes de contenido: `{spacing.margin-desktop}` (44px) en escritorio, `{spacing.margin-mobile}` (20px) en móvil — valores tomados literalmente del mock (`.hero-head`, `.filters`, `.cards` vs. `.phone-topbar`, `.phone-filters`, `.phone-cards`). Layout de una sola columna en ambas superficies; el producto no usa grillas multi-columna — ni siquiera la tabla comparativa, que es una grilla de máximo 4 columnas (1 etiqueta + hasta 3 vehículos, tope confirmado de comparación simultánea).

En móvil, la barra fija de comparación (`compare-footer-bar`) se ancla al fondo de la pantalla sobre el contenido con scroll; en escritorio vive dentro del marco de la página, no fija. Los filtros son una fila horizontal con scroll propio en ambas superficies, con `{spacing.gutter}` (24px) entre `filter-chip` consecutivos y entre las columnas de vehículo de `comparison-table` en escritorio (ver `EXPERIENCE.md` → Interaction Primitives para el patrón de interacción completo, no solo la fila visible).

## Elevation & Depth

Elevación mínima, casi ausente — coherente con la disciplina editorial de la dirección Premium. Las tarjetas de resultado **no** llevan sombra; se separan por `{colors.border}` de 1px. La sombra se reserva exclusivamente para "marcos" completos: el contenedor de navegador/app en las composiciones del mock, paneles modales/drawer y el panel de estado vacío.

- `shadow-frame` (claro): `0 24px 60px rgba(18, 24, 31, 0.18)` — tinta de `{colors.ink}` a baja opacidad, ambient, sin dirección dura.
- `shadow-frame-dark`: `0 24px 60px rgba(0, 0, 0, 0.45)` — en oscuro una sombra tintada no se lee contra `{colors.background-dark}`; se usa negro puro a mayor opacidad para mantener la separación de plano.

Nada usa elevación como jerarquía de contenido (eso es trabajo de la tipografía y el espacio, no de la sombra).

## Shapes

Esquinas deliberadamente pequeñas en controles (`{rounded.sm}`, 2px) — el registro es "preciso", no "amigable"; nada de pastillas en botones de escritorio. La escala completa:

| Token | Valor | Uso |
|---|---|---|
| `{rounded.sm}` | 2px | Botones, badges, `compare-select-button`, controles de formulario |
| `{rounded.DEFAULT}` | 4px | Elementos chip pequeños de sistema (p. ej. pill de URL en el mock, no visible en producto) |
| `{rounded.md}` | 6px | Paneles internos, banners informativos |
| `{rounded.lg}` | 10px | Contenedores de marco completo: modal, drawer de filtros, panel de estado vacío |
| `{rounded.xl}` | 30px | Marco de dispositivo / superficie de pantalla completa en contextos de app móvil nativa |
| `{rounded.full}` | 9999px | `filter-chip` en su variante píldora (uso denso móvil) |

La única forma "suave" del sistema es el chip de filtro en píldora; todo lo demás — tarjetas, botones, badges, tabla — se mantiene en el registro casi-recto de `{rounded.sm}`–`{rounded.md}`.

## Components

Implementación destino: **Angular** (última versión) sobre **PrimeNG**. Estos tokens no son valores por defecto de PrimeNG — son la propiedad intelectual de marca de `[Proyecto Auto]` y se inyectan en tiempo de implementación vía un `definePreset` (basado en Aura) que mapea `semantic.colorScheme.light` / `semantic.colorScheme.dark` a los hex de este documento, con `darkModeSelector` basado en clase (el modo oscuro es una decisión explícita del usuario o la app, no solo `prefers-color-scheme`). PrimeNG resuelve mecánica de componente (Table para la comparación, Drawer/Dialog para filtros, InputText/FloatLabel para el formulario de cuenta); este documento resuelve identidad visual encima de esa mecánica.

- **`vehicle-result-card`** — Anatomía: nombre (`{typography.title}` / `{typography.title-sm}` en móvil), agencia (`{typography.body}`, `{colors.ink-secondary}`), hasta 2–3 badges de equipamiento, precio (`{typography.price}`, `{colors.accent}`) con calificador "MXN aprox." (`{typography.caption}`), `compare-select-button` a la derecha. Fondo `{colors.surface}`, separador inferior `{colors.border}` de 1px — nunca sombra, nunca radio de esquina propio (es una fila, no una tarjeta flotante).
- **`filter-chip`** — Fila horizontal con scroll propio. Estado inactivo: texto `{colors.ink-secondary}`, sin relleno. Estado activo/con valor aplicado: relleno `{colors.ink}`, texto `{colors.surface}` (variante móvil en píldora, `{rounded.full}`); variante escritorio usa divisor vertical fino en vez de píldora, igual que el mock. Un chip por dimensión de filtro (año, precio, carrocería, transmisión/motor, equipamiento, color, disponibilidad estimada).
- **`compare-select-button`** — Botón de línea (`{colors.ink}` de borde y texto, fondo transparente) con la etiqueta "Agregar a comparar". Estado seleccionado invierte a relleno `{colors.ink}` / texto `{colors.surface}` con la etiqueta "En comparación ✓". Ver `EXPERIENCE.md` para el comportamiento en el tope de 3.
- **`compare-footer-bar`** — Banda fija (móvil) o de fin de lista (escritorio). Claro: relleno `{colors.ink}`, texto `{colors.ink-dark}` (tinta clara sobre banda oscura). Oscuro: en vez de una banda "aún más oscura" que el fondo ya oscuro, usa `{colors.surface-dark}` con borde superior `{colors.border-dark}` — mismo tratamiento que la variación 2 del explorador de paletas. CTA "Comparar" siempre en `{colors.accent}` / `{colors.accent-foreground}`.
- **`comparison-table`** — **Escritorio:** grilla de hasta 4 columnas (1 etiqueta + máx. 3 vehículos). Encabezado con borde inferior grueso en `{colors.ink}`, nombre de vehículo en `{typography.title}`, agencia en `{typography.caption}`/`{colors.ink-secondary}`. Columna de etiqueta de fila en mayúsculas, `{colors.ink-secondary}`, `{typography.label}`. Filas separadas por `{colors.border}` de 1px. **Móvil:** una tarjeta apilada por vehículo — fondo `{colors.surface}`, borde `{colors.border}` de 1px, radio `{rounded.md}` (6px); dentro, nombre en `{typography.title}` con precio en `{typography.price}`/`{colors.accent}` arriba, seguido de filas etiqueta/valor (misma tipografía que la vista de escritorio) separadas por `{colors.border}`. Tarjetas apiladas con `{spacing.6}` (24px) de separación vertical.
  → Referencia visual: [`comparacion-movil-propuestas.html`](https://claude.ai/code/artifact/d7fe43d6-4b26-4b6d-8906-288e16833697) — Propuesta B, elegida tras comparar 4 layouts móviles.
- **`availability-badge`** — Texto + borde en `{colors.success}` (nunca relleno sólido), `{typography.caption}`, `{rounded.sm}`. Va siempre acompañado del microcopy "estimada" — nunca aparece solo (ver `EXPERIENCE.md` → State Patterns, dato de frescura semanal).
- **`account-form-field`** — Input de línea: borde `{colors.border}` en reposo, `{colors.accent}` en foco (2px), etiqueta en `{typography.label}` sobre el campo. Usado solo en el gate de creación de cuenta (email o teléfono) — nunca antes, dado que buscar/filtrar/comparar es anónimo.
- **`button-primary`** — Relleno `{colors.accent}`, texto `{colors.accent-foreground}`, `{typography.label}`, `{rounded.sm}`. Reservado a la acción principal de la pantalla ("Comparar", "Guardar comparación", "Crear cuenta").
- **`button-secondary`** — Línea `{colors.ink}`, fondo transparente, texto `{colors.ink}`. Todo lo que no es la acción principal ("Ajustar filtros", "Cancelar", "Iniciar sesión" cuando comparte pantalla con "Crear cuenta").
- **`empty-state-panel`** — Fondo `{colors.background}`, filete corto centrado en `{colors.accent}` sobre el titular (`{typography.display-sm}`), cuerpo en `{typography.body}`/`{colors.ink-secondary}`, un solo `button-secondary` como acción. Cubre "sin resultados de búsqueda" y "sin comparaciones guardadas".

→ Referencia visual: [`mockups/direction-premium.html`](mockups/direction-premium.html) para anatomía y composición completa de cada componente (tarjeta, chip, footer de comparación, tabla, estado vacío, escritorio + móvil); [`mockups/color-themes-premium.html`](mockups/color-themes-premium.html) Variación 2 para el fragmento de tarjeta + footer recoloreado con la paleta elegida en ambos modos.

## Do's and Don'ts

| Do | Don't |
|---|---|
| Separadores de 1px (`{colors.border}`) entre filas de tarjetas | Sombra propia en tarjetas de resultado — eso es la dirección "fresco/directo" descartada |
| `{colors.success}` solo en `availability-badge`, siempre junto al microcopy "estimada" | Usar verde para "guardado", "válido" u otro estado genérico |
| Serif (`Georgia`) solo en nombre de vehículo, precio y titulares | Serif en botones, chips, badges o cualquier control interactivo |
| Esquinas de 2px en controles — el registro es preciso | Botones o tarjetas en píldora fuera de `filter-chip` móvil |
| Modo oscuro con `darkModeSelector` de clase, banda de comparación pasa a `{colors.surface-dark}` + borde | Modo oscuro solo por `prefers-color-scheme`, o una banda "más negra que el fondo" en oscuro |
| Un acento cromático (`{colors.accent}`) para precio y CTA primario | Introducir un segundo color de marca o degradado |
