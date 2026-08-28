---
title: 'CI — build y test en cada push/PR'
type: 'feature'
created: '2026-08-24'
status: 'done'
review_loop_iteration: 0
baseline_commit: '8ae671962801543d7ac5242aad84cdabd24ad2a3'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-auto-2026-08-19/ARCHITECTURE-SPINE.md'
---

> **Nota (2026-08-27, code review):** `status: 'done'` refleja que el ciclo de esta spec terminó, pero una tarea y un AC quedan intencionalmente sin cumplir -- **no** por un olvido: branch protection en `main` está bloqueada por el plan de GitHub (repos privados en Free no soportan required status checks), no por falta de implementación. Ver el ítem sin marcar en `## Tasks & Acceptance` y `deferred-work.md` para el detalle. La verificación manual "Revisar settings de branch protection" en `## Verification` fallará mientras esto siga sin resolverse.

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No existe pipeline de CI; código roto (.NET o Angular) puede llegar a cualquier rama o PR sin detección automática, violando AD-17 ("ninguna historia de backend se considera hecha sin pasar por el pipeline").

**Approach:** Workflow de GitHub Actions (`.github/workflows/ci.yml`) con dos jobs paralelos — `backend` (.NET) y `frontend` (Angular) — que compilan y corren pruebas en cada push a cualquier rama y en cada PR, configurados como status checks requeridos para bloquear el merge si fallan.

## Boundaries & Constraints

**Always:**
- El workflow dispara en `push` (cualquier rama) y en `pull_request` — no solo en `main`.
- Backend: `actions/setup-dotnet` lee la versión desde `global.json` (10.0.301); pasos `dotnet restore` → `dotnet build --no-restore` → `dotnet test --no-build` contra `Auto.slnx`.
- Frontend: `actions/setup-node` con `node-version-file: web/.nvmrc`; pasos `npm ci` → `npm run build` → `npm test` (corre `ng test`, backed by vitest) desde `web/`.
- Se crea `tests/Api.Tests` (xunit) con una prueba trivial que pase, referenciando `src/Api`, agregada a `Auto.slnx` — sin esto `dotnet test` no ejecuta nada real (decisión confirmada por el humano).
- `backend` y `frontend` son jobs independientes (no steps de un job) para que corran en paralelo y aparezcan como status checks separados en el PR.

**Ask First:**
- Mecanismo para bloquear merge si el pipeline falla (AD-17): ¿configurar branch protection real en `main` vía `gh api` (requiriendo los status checks `backend`/`frontend`), o solo documentar el paso manual en el README y dejar que el humano lo active desde GitHub? — HALT antes de mutar settings del repo real. **[RESUELTO 2026-08-24]:** `gh api`, pero recién después del primer push real que ejercite el workflow (GitHub no ofrece `backend`/`frontend` como checks seleccionables hasta que corrieron al menos una vez); no se activa en esta historia porque step-03 prohíbe push/remote ops.
- Nombres de job/status-check propuestos: `backend`, `frontend` — confirmar antes de fijarlos en branch protection. **[RESUELTO 2026-08-24]:** confirmados por el humano, sin cambios.

**Never:**
- No se ejecuta `terraform plan`/`apply` en este workflow (CD real es historia 1.4).
- No se agregan pruebas de features reales — solo el placeholder trivial en `Api.Tests`; pruebas de funcionalidad llegan con cada historia que la implemente.
- No se sube ni cachea artefacto de build hacia Azure en este workflow.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Push con ambos stacks verdes | push a rama feature | Jobs `backend` y `frontend` corren y terminan verdes | N/A |
| PR con `Api.Tests` roto | PR abierto, prueba .NET falla | Job `backend` falla; status check rojo | Merge bloqueado (branch protection) |
| PR con `ng test` roto | PR abierto, prueba Angular falla | Job `frontend` falla; status check rojo | Merge bloqueado (branch protection) |

</frozen-after-approval>

## Code Map

- `.github/workflows/ci.yml` -- (nuevo) workflow con jobs `backend`/`frontend`
- `Auto.slnx` -- solución completa; agregar `tests/Api.Tests` aquí para que `dotnet build/test` la incluya
- `global.json` -- fija SDK .NET `10.0.301`, `rollForward: latestFeature`; `actions/setup-dotnet` debe leerlo
- `src/Directory.Build.props:4` -- `TargetFramework net10.0` centralizado, heredado por el nuevo proyecto de tests
- `src/Api/Api.csproj` -- proyecto a referenciar desde `tests/Api.Tests`
- `tests/Api.Tests/Api.Tests.csproj` -- (nuevo) proyecto xunit
- `web/package.json` -- scripts `build` (`ng build`) / `test` (`ng test`); `packageManager: npm@11.13.0`
- `web/angular.json` -- `architect.test` usa builder `@angular/build:unit-test` (vitest + jsdom, sin Karma)
- `web/.nvmrc` -- Node `24.17.0`
- `README.md` (raíz) -- ya documenta build backend/frontend; falta sección de test y CI
- Referencia normativa: `epics.md` Story 1.3 AC (líneas 163-174); `epic-1-context.md` Cross-Story Dependencies (1.3 es prerequisito de 1.4)

## Tasks & Acceptance

**Execution:**
- [x] `tests/Api.Tests/Api.Tests.csproj` -- crear proyecto xunit (net10.0) referenciando `src/Api/Api.csproj` -- da a `dotnet test` algo real que ejecutar
- [x] `tests/Api.Tests/PlaceholderTests.cs` -- una prueba trivial que pase -- valida el pipeline end-to-end (falla si algo lo rompe deliberadamente)
- [x] `Auto.slnx` -- agregar `tests/Api.Tests` a la solución -- para que `dotnet build/test Auto.slnx` lo incluya
- [x] `.github/workflows/ci.yml` -- trigger `push` (cualquier rama) + `pull_request`; job `backend` (setup-dotnet vía `global.json`, restore/build/test); job `frontend` (setup-node vía `web/.nvmrc`, `npm ci`/build/test) -- cumple el AC de compilar+probar ambos stacks en cada push/PR
- [ ] Branch protection en `main` -- requerir status checks `backend`/`frontend` antes de permitir merge -- cumple AD-17; **intentado y bloqueado por el plan de GitHub**: tras el primer push real (commit `d6c386a`, `backend`/`frontend` en verde), `gh api .../protection` devolvió `403 "Upgrade to GitHub Pro or make this repository public"` — repos privados en plan Free no soportan required status checks. No es un gap de configuración; requiere que el humano decida (GitHub Pro o repo público). Documentado en `README.md` § CI y `deferred-work.md`
- [x] `README.md` -- documentar cómo correr build+test localmente y cómo activar branch protection -- reproducibilidad (sigue el patrón de la historia 1.2)

**Acceptance Criteria:**
- Given un push a cualquier rama, when se dispara el workflow, then los jobs `backend` y `frontend` compilan y corren sus pruebas sin intervención manual.
- Given un PR abierto, when el workflow se dispara, then ambos jobs corren como status checks visibles en el PR.
- Given un PR con `Api.Tests` o `ng test` fallando, when se intenta fusionar, then GitHub bloquea el merge hasta que el pipeline esté en verde.
- Given `Auto.slnx`, when se ejecuta `dotnet build` localmente, then incluye `tests/Api.Tests` sin errores de referencia.

### Review Findings

- [x] [Review][Patch] Guard `if: !github.event.deleted` no tiene efecto real [`.github/workflows/ci.yml:16`] — confirmado empíricamente (push+delete de una rama de prueba): ningún run se dispara para un push que borra una rama, con o sin `branches: ['**']`, así que la condición nunca se evalúa contra un run real. **Aplicado:** guard eliminado de ambos jobs.
- [x] [Review][Patch] Comando local de test en README no reproduce la config de CI [`README.md:40`] — `dotnet test Auto.slnx` local corre en `Debug`; CI usa `--configuration Release`. **Aplicado:** nota agregada aclarando la diferencia.
- [x] [Review][Patch] `status: 'done'` en el frontmatter luce contradictorio junto a una tarea sin marcar y un AC sin cumplir [spec frontmatter] — el Acceptance Auditor señaló que, sin contexto, parece un olvido en vez de un bloqueo de plataforma ya documentado. **Aplicado:** nota aclaratoria agregada arriba del frontmatter.
- [x] [Review][Defer] Terraform (`infra/terraform/`) sin ninguna validación en CI (`fmt`/`validate`/`tflint`) [`infra/terraform/`] — deferred, pre-existing (historia 1.2, no causado por esta historia)
- [x] [Review][Defer] `cancel-in-progress: true` cancelará runs de `main` una vez que dispare CD real (historia 1.4), lo cual se ve igual que un fallo en Checks [`.github/workflows/ci.yml:10`] — deferred, sin consecuencia hoy
- [x] [Review][Defer] Addendum al gap de branch protection ya registrado: el payload documentado fija `enforce_admins=false`, así que un admin podría fusionar en rojo una vez activado — deferred junto con la decisión de plan/visibilidad de GitHub

## Spec Change Log

- 2026-08-24 -- Implementación ejecutada: `tests/Api.Tests` (xunit), `Auto.slnx` actualizado, `.github/workflows/ci.yml` (jobs `backend`/`frontend`), `README.md` actualizado. Branch protection en `main` **no** se activó (item Ask First del spec) -- requiere que un humano confirme el mecanismo (branch protection real vía `gh api`/UI) antes de mutar settings del repo real; ver README § CI para el paso manual documentado.
- 2026-08-24 -- Code review: patches aplicados (`concurrency`, `permissions`, `timeout-minutes`, guard de rama borrada en `ci.yml`; `tests/Directory.Build.props` para eliminar duplicación de `TargetFramework`/`Nullable`; payload `gh api` completo en README). Sin `intent_gap`/`bad_spec`; 7 hallazgos reales no bloqueantes van a `deferred-work.md`.
- 2026-08-24 -- Commit `d6c386a` pusheado a `main` (confirmado por el humano). `backend`/`frontend` corrieron en verde en GitHub. Intento de activar branch protection vía `gh api` bloqueado por el plan de GitHub (`403`, repos privados en Free no soportan required status checks) -- no es un gap de spec/código; queda como decisión pendiente del humano (GitHub Pro vs. repo público), documentada en README § CI y `deferred-work.md`.
- 2026-08-27 -- `/bmad-code-review` (segunda pasada, 4 capas incl. Acceptance Auditor): 3 patches aplicados -- guard muerto `if: !github.event.deleted` eliminado de `ci.yml` (confirmado empíricamente: un push que borra una rama nunca dispara ningún run, con o sin `branches: ['**']`), nota de configuración `--configuration Release` agregada al comando de test local en README, y nota aclaratoria agregada arriba del frontmatter explicando por qué `status: 'done'` convive con una tarea/AC sin cumplir. 2 hallazgos reales no bloqueantes (Terraform sin cobertura de CI, semántica de `cancel-in-progress` sobre `main` de cara a la historia 1.4) y 1 addendum al gap de branch protection ya registrado (`enforce_admins`) van a `deferred-work.md`. Sin `intent_gap`/`bad_spec`.

## Design Notes

Dos jobs (`backend`, `frontend`) en vez de steps secuenciales de un solo job: GitHub Actions los corre en paralelo por default y cada uno aparece como status check independiente en el PR — necesario para exigir branch protection granular sin que un fallo de un stack oculte o bloquee el reporte del otro.

## Verification

**Commands:**
- `dotnet build Auto.slnx` -- expected: build exitoso, incluye `tests/Api.Tests`
- `dotnet test Auto.slnx` -- expected: `Api.Tests` corre y pasa (1+ prueba verde)
- `npm ci && npm test` (desde `web/`) -- expected: vitest corre y pasa
- Push de prueba a una rama y revisar la pestaña Actions -- expected: jobs `backend` y `frontend` en verde

**Manual checks (if no CLI):**
- Revisar `.github/workflows/ci.yml`: ambos jobs disparan en `push` y `pull_request`, no solo `push` a `main`.
- Revisar settings de branch protection de `main` en GitHub: status checks `backend`/`frontend` marcados como requeridos.

## Suggested Review Order

**Pipeline de CI (punto de entrada)**

- Entry point: ambos jobs (`backend`/`frontend`) disparan en push (cualquier rama) y en PR, en paralelo e independientes entre sí.
  [`ci.yml:3`](../../.github/workflows/ci.yml#L3)

- Hardening agregado en review: `concurrency` cancela runs superados, `permissions` de solo lectura, guard contra push de rama borrada.
  [`ci.yml:8`](../../.github/workflows/ci.yml#L8)

- Job `backend`: restore/build/test contra `Auto.slnx` con el SDK fijado por `global.json`.
  [`ci.yml:16`](../../.github/workflows/ci.yml#L16)

- Job `frontend`: `setup-node` vía `.nvmrc`, build+test de Angular con caché de npm.
  [`ci.yml:36`](../../.github/workflows/ci.yml#L36)

**Scaffolding de pruebas .NET (nuevo, requerido por el AC)**

- Proyecto xUnit mínimo para que `dotnet test` tenga algo real que ejecutar — hoy no existía ningún proyecto de pruebas .NET.
  [`Api.Tests.csproj:1`](../../tests/Api.Tests/Api.Tests.csproj#L1)

- `TargetFramework`/`Nullable` heredados de `src/Directory.Build.props` en vez de duplicados (fix de review), evita drift entre ambos.
  [`Directory.Build.props:3`](../../tests/Directory.Build.props#L3)

- `Auto.slnx` incluye el nuevo proyecto en una carpeta de solución `/tests/` para que build/test lo alcancen.
  [`Auto.slnx:8`](../../Auto.slnx#L8)

**Periféricos**

- Prueba trivial placeholder, a reemplazar con cobertura real conforme aterricen features en `src/Api`.
  [`PlaceholderTests.cs:9`](../../tests/Api.Tests/PlaceholderTests.cs#L9)

- Documentación de build/test local, del pipeline de CI, y runbook manual (+ payload `gh api`) para activar branch protection cuando exista el primer run real.
  [`README.md:38`](../../README.md#L38)
