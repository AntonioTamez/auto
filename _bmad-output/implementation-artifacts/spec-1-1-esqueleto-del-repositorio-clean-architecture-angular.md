---
title: 'Esqueleto del repositorio (Clean Architecture + Angular)'
type: 'feature'
created: '2026-08-20'
status: 'done'
review_loop_iteration: 1
baseline_commit: '7be8467c7e01af98b0bebc19feb68e1bb82ca992'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-auto-2026-08-19/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El repositorio no tiene código de aplicación todavía (solo tooling de planificación BMAD). Toda historia futura de las 6 épicas necesita una estructura base consistente de backend y frontend sobre la cual construir, sin deuda de reestructuración posterior.

**Approach:** Scaffold de un backend .NET 10 / ASP.NET Core con las 4 capas de Clean Architecture (`Domain`/`Application`/`Infrastructure`/`Api`) respetando la regla de dependencias hacia adentro, y un workspace Angular independiente en el mismo repo. Ambos deben compilar localmente sin errores.

## Boundaries & Constraints

**Always:**
- `Domain` no referencia ningún paquete de EF Core, Npgsql, ni ningún otro paquete de infraestructura (AD-1, invariante duro).
- Estructura de carpetas del backend exactamente: `src/Domain`, `src/Application`, `src/Infrastructure`, `src/Api` (Structural Seed del Architecture Spine).
- Reglas de dependencia: `Application`→`Domain`; `Infrastructure`→`Application`+`Domain`; `Api`→`Application` (compone DI e integra Infrastructure).
- Angular vive en `web/`, en la raíz del repo, hermano de `src/`, como codebase independiente (sin código compartido con el futuro Flutter, AD-2). Decisión confirmada por el humano.
- Se agrega un `.gitignore` en la raíz que cubra artefactos .NET (`bin/`, `obj/`, `*.user`) y Node/Angular (`node_modules/`, `dist/`, `.angular/`) antes de comitear el scaffold.
- Todos los proyectos .NET targetean `net10.0`.

**Ask First:**
- Si se crea un `.sln` explícito en la raíz agrupando los 4 proyectos .NET, o si el build se maneja solo por carpeta (`dotnet build` dentro de cada proyecto) — la spine no lo especifica. **[RESUELTO 2026-08-20 vía code review]:** se crea `Auto.slnx` en la raíz agrupando los 4 proyectos (formato `.slnx` nativo del SDK .NET 10, equivalente moderno al `.sln` clásico). Confirmado por el humano.

**Never:**
- No implementar entidades de dominio reales, casos de uso, endpoints funcionales, Terraform ni pipelines CI/CD — corresponden a las historias 1.2 en adelante.
- No configurar PrimeNG, theming, ni routing de Angular todavía — el AC de esta historia solo exige que el shell compile, no funcionalidad de UI.
- No compartir código entre el shell Angular y el futuro cliente Flutter (fuera de alcance, AD-2).

</frozen-after-approval>

## Code Map

- `src/Domain/Domain.csproj` -- class library .NET 10 nueva, capa base sin dependencias hacia afuera (ancla del Dependency Rule, AD-1)
- `src/Application/Application.csproj` -- class library .NET 10 nueva, referencia `Domain`
- `src/Infrastructure/Infrastructure.csproj` -- class library .NET 10 nueva, referencia `Application` y `Domain`
- `src/Api/Api.csproj` -- proyecto ASP.NET Core Web API .NET 10 nuevo, referencia `Application`, composition root/DI
- `web/` -- workspace Angular nuevo (`ng new`), independiente del backend
- `.gitignore` -- nuevo, cubre artefactos .NET y Node/Angular
- Referencia normativa: `_bmad-output/planning-artifacts/architecture/architecture-auto-2026-08-19/ARCHITECTURE-SPINE.md`, sección `## Backend source tree` (Structural Seed) y tabla `## Stack` (versiones)
- Tooling local confirmado disponible en este entorno: .NET SDK `10.0.301`, Angular CLI `22.1.3`, Node `v24.17.0`, npm `11.13.0` -- `dotnet new` y `ng new` corren directo, sin instalaciones adicionales

## Tasks & Acceptance

**Execution:**
- [x] `src/Domain/Domain.csproj` -- crear class library .NET 10 vacía -- capa base, cero dependencias hacia afuera
- [x] `src/Application/Application.csproj` -- crear class library .NET 10, agregar referencia a `Domain` -- capa de casos de uso/interfaces
- [x] `src/Infrastructure/Infrastructure.csproj` -- crear class library .NET 10, agregar referencias a `Application` y `Domain` -- capa de implementaciones
- [x] `src/Api/Api.csproj` -- crear proyecto ASP.NET Core Web API .NET 10, agregar referencia a `Application` -- composition root
- [x] `web/` -- generar workspace Angular con `ng new` -- shell frontend independiente
- [x] `.gitignore` -- crear en la raíz cubriendo `bin/`, `obj/`, `*.user`, `node_modules/`, `dist/`, `.angular/`
- [x] Verificar `dotnet build` sobre los 4 proyectos -- compila sin errores
- [x] Verificar `ng build` (o `npm run build`) dentro de `web/` -- compila sin errores

**Acceptance Criteria:**
- Given un repositorio vacío, when se hace scaffold del backend y del frontend, then ambos proyectos compilan localmente sin errores.
- Given el scaffold del backend, when se revisa la estructura de carpetas, then coincide con el Structural Seed del Architecture Spine (`src/Domain`, `src/Application`, `src/Infrastructure`, `src/Api`) y `Domain` no tiene ninguna dependencia hacia afuera.

### Review Findings

- [x] [Review][Decision] Decisión de `.sln` no confirmada por el humano — el spec marca explícitamente esta elección como "Ask First" (`spec-1-1...md:32`), pero el diff resuelve la ambigüedad de forma unilateral (sin `.sln`, build por carpeta, documentado en `README.md:78-93`) sin evidencia de que se haya pedido confirmación humana. **Resuelto:** el humano confirmó crear `Auto.slnx` (equivalente moderno al `.sln`), agrupando los 4 proyectos; ver `Ask First` arriba y `Auto.slnx`.
- [x] [Review][Patch] Routing de Angular configurado pese a la restricción "Never" del spec [web/src/app/app.config.ts:2,9, web/src/app/app.ts:2,7, web/src/app/app.html:3] — el spec prohíbe explícitamente configurar routing en esta historia ("No configurar ... routing de Angular todavía"), pero `ng new` se generó con routing habilitado (`provideRouter`, `RouterOutlet`, `<router-outlet />`), contradiciendo también el propio "Suggested Review Order" del spec que afirma "sin ... routing todavía". **Resuelto:** se retiró `provideRouter`/`RouterOutlet`/`<router-outlet />` y se eliminó `app.routes.ts` (sin uso).
- [x] [Review][Patch] Estado del spec desincronizado con sprint-status.yaml [spec-1-1...md:5] — el frontmatter declara `status: 'done'` mientras `sprint-status.yaml` marca la historia como `review`; deben coincidir. **Resuelto:** frontmatter puesto en `review`; se sincroniza a `done` en ambos archivos al cerrar esta revisión.
- [x] [Review][Patch] `web/package.json` sin salto de línea final [web/package.json:32] — contradice la regla propia `insert_final_newline = true` de ambos `.editorconfig` del repo. **Resuelto.**
- [x] [Review][Patch] `main.ts` solo hace `console.error` si falla el bootstrap [web/src/main.ts:5-6] — el usuario ve una página en blanco sin indicación de fallo. **Resuelto:** se agrega `document.body.textContent = 'Failed to start application'` en el `.catch`.
- [x] [Review][Patch] `README.md` no documenta cómo correr la API [README.md:78-93] — faltan `dotnet restore`, `dotnet run --project src/Api`, y referencia cruzada a las versiones ya fijadas en `global.json`/`web/.nvmrc`. **Resuelto** al documentar el nuevo flujo de build vía `Auto.slnx`.
- [x] [Review][Defer] Sin `.gitattributes`; `end_of_line = crlf` fijado repo-wide en `.editorconfig` sin política para runners Linux en CI [.editorconfig:8] — deferred, pre-existing
- [x] [Review][Defer] RFC 7807 Problem Details no configurado en `Program.cs` pese a que `epic-1-context.md` lo señala como decisión de scaffold [src/Api/Program.cs:1-17] — deferred, pre-existing
- [x] [Review][Defer] Middleware de validación JWT centralizado ausente, sin registrar como diferido [src/Api/Program.cs:1-17] — deferred, pre-existing
- [x] [Review][Defer] No existe proyecto de tests .NET; la historia 1.3 (CI build+test) no tendrá qué correr del lado backend [src/] — deferred, pre-existing
- [x] [Review][Defer] `.gitignore` raíz no cubre patrones de secretos/env más allá de lo exigido por el spec [.gitignore:1-23] — deferred, pre-existing
- [x] [Review][Defer] BOM UTF-8 inconsistente entre archivos generados (`Domain.csproj`, `Application.csproj`, `Infrastructure.csproj`, `launchSettings.json` lo tienen; `Api.csproj`, `Program.cs` no) [src/Domain/Domain.csproj:1] — deferred, pre-existing
- [x] [Review][Defer] `Program.cs` usa `UseHttpsRedirection()` sin `UseForwardedHeaders`, causará redirect-loop detrás de un reverse proxy (Azure Container Apps) [src/Api/Program.cs:15] — deferred, pre-existing
- [x] [Review][Defer] `global.json` con `rollForward: latestFeature` puede fallar en máquinas de CI con otro feature band del SDK 10 [global.json:3-4] — deferred, pre-existing

## Spec Change Log

## Verification

**Commands:**
- `dotnet build` (ejecutado sobre cada `.csproj` o sobre el `.sln` si se crea) -- expected: build exitoso, 0 errores
- `ng build` (o `npm run build`) desde `web/` -- expected: build exitoso, 0 errores

**Manual checks (if no CLI):**
- Revisar `src/Domain/Domain.csproj` y su código: no debe tener ninguna referencia de paquete NuGet de infraestructura (EF Core, Npgsql, etc.)

## Suggested Review Order

**Arquitectura del backend (Clean Architecture)**

- Ancla del Dependency Rule (AD-1): capa base sin ninguna referencia hacia afuera.
  [`Domain.csproj:1`](../../src/Domain/Domain.csproj#L1)

- Convenciones MSBuild (target/nullable/usings) centralizadas para las 4 capas, en vez de duplicadas.
  [`Directory.Build.props:3`](../../src/Directory.Build.props#L3)

- Capa de casos de uso/interfaces; única referencia hacia `Domain`.
  [`Application.csproj:4`](../../src/Application/Application.csproj#L4)

- Capa de implementaciones; referencia `Application` + `Domain` según la regla de dependencia.
  [`Infrastructure.csproj:4`](../../src/Infrastructure/Infrastructure.csproj#L4)

- Composition root; referencia solo `Application` a propósito — todavía no compone `Infrastructure` (ver `deferred-work.md`).
  [`Api.csproj:8`](../../src/Api/Api.csproj#L8)

**Shell de Angular**

- Placeholder de `ng new` recortado a un shell mínimo; sin theming/routing todavía, fuera de alcance de esta historia.
  [`app.html:1`](../../web/src/app/app.html#L1)

**Higiene de repo y reproducibilidad**

- Pinea el SDK de .NET exacto verificado en este entorno para builds reproducibles.
  [`global.json:3`](../../global.json#L3)

- Pinea la versión de Node verificada, complementando el `packageManager` de npm ya fijado en `package.json`.
  [`.nvmrc:1`](../../web/.nvmrc#L1)

- Cubre artefactos de build .NET y Node/Angular antes del primer commit.
  [`.gitignore:1`](../../.gitignore#L1)

- Documenta el layout del monorepo y cómo compilar ambos lados.
  [`README.md:1`](../../README.md#L1)
