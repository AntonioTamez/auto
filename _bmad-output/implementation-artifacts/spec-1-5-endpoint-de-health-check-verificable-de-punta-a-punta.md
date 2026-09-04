---
title: 'Endpoint de health-check verificable de punta a punta'
type: 'feature'
created: '2026-09-01'
status: 'done'
review_loop_iteration: 0
baseline_commit: '22128485adc49fcab1d5e5f6578874384c4684ad'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-auto-2026-08-19/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El esqueleto (.NET + Angular) ya se despliega a Azure (historia 1.4), pero nada prueba que la cadena completa código → CI → Terraform → Azure → app corriendo funcione de punta a punta: no hay un endpoint real que la API sirva ni que Angular consuma.

**Approach:** Agregar `GET /health` en la API (200 + JSON de estado, sin auth), que el Container App exponga probes de liveness/readiness contra ese mismo endpoint, y que el shell de Angular lo llame por HTTP y muestre visualmente el resultado — confirmando la cadena end-to-end.

## Boundaries & Constraints

**Always:**
- Angular llama a la API vía **CORS directo al FQDN del Container App** (`https://<container-app-fqdn>/health`) — decisión de esta sesión: el linked backend nativo de Azure Static Web Apps no tiene soporte en el provider `azurerm` para Container Apps (solo Function Apps, vía `azurerm_static_web_app_function_app_registration`); usarlo requeriría el provider `azapi` (recursos REST crudos, sin schema), fuera de alcance para este scaffold.
- CORS en `Program.cs` (`AddCors`/`UseCors`) permite únicamente los orígenes conocidos — nunca `AllowAnyOrigin` en el ambiente desplegado. El origen real (`azurerm_static_web_app.main.default_host_name`) se inyecta al Container App vía variable de entorno desde Terraform; local (`ng serve`, puerto 4200) se permite solo en `appsettings.Development.json`.
- El Container App agrega `liveness_probe`/`readiness_probe` (HTTP, puerto 8080, path `/health`) -- cierra el gap diferido explícitamente por la historia 1.4 ("sin probe... deferred a la historia 1.5").
- La URL base de la API en Angular (`web/src/environments/environment.ts`) se sobreescribe en `cd-dev.yml` con el FQDN real **después** de `terraform apply` y **antes** de `npm run build` -- el FQDN no existe hasta ese apply, así que no puede fijarse en el código fuente commiteado.
- `/health` responde 200 con un JSON simple (`{status, timestamp}`) -- no es un error, así que el envelope RFC 7807 (Consistency Conventions del spine) no aplica aquí.

**Ask First:**
- Ninguno pendiente -- ambas decisiones de arquitectura de esta historia (CORS vs. linked backend) ya se resolvieron con el humano en esta sesión.

**Never:**
- No se agregan endpoints de negocio nuevos más allá de `/health`.
- No se usa el provider `azapi` ni el linked backend de Azure Static Web Apps.
- `/health` no requiere autenticación ni JWT.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| GET `/health` desde el origen de la SWA | Preflight + GET, origen en la allowlist de CORS | 200 + JSON de estado, headers CORS presentes | N/A |
| GET `/health` desde un origen no listado | Ej. un tercero probando el FQDN directo desde otro dominio | El servidor responde 200 igual, pero el navegador bloquea la respuesta por CORS (comportamiento nativo, no código propio) | N/A |
| Angular carga durante cold-start del Container App (`min_replicas=0`) | Primer request tras estar en cero | El shell muestra un estado de carga/error, nunca una pantalla en blanco | Manejo de error explícito en la llamada HTTP (no unhandled rejection) |

</frozen-after-approval>

## Code Map

- `src/Api/Program.cs` -- agregar `AddCors`/`UseCors` (origen desde configuración) y `MapGet("/health", ...)` devolviendo `{status, timestamp}` -- hoy el archivo solo tiene `AddOpenApi`/`UseHttpsRedirection`, sin CORS ni endpoints.
- `src/Api/appsettings.Development.json` -- agregar el origen `http://localhost:4200` para que `ng serve` local funcione sin desplegar.
- `infra/terraform/main.tf` -- en `azurerm_container_app.api.template.container`: agregar `env { name = "Cors__AllowedOrigins__0", value = "https://${azurerm_static_web_app.main.default_host_name}" }`, más bloques `liveness_probe`/`readiness_probe` (HTTP, `port = 8080`, `path = "/health"`).
- `web/src/environments/environment.ts` -- (nuevo) `export const environment = { apiBaseUrl: 'http://localhost:5075' }` -- default para desarrollo local, coincide con el perfil `http` de `launchSettings.json`.
- `web/src/app/app.config.ts` -- agregar `provideHttpClient()` a `providers` -- hoy solo tiene `provideBrowserGlobalErrorListeners()`.
- `web/src/app/app.ts` / `web/src/app/app.html` -- llamar `GET {environment.apiBaseUrl}/health` (vía `HttpClient`, expuesto como signal) y mostrar el estado -- hoy solo renderiza `Hello, {{title()}}`.
- `.github/workflows/cd-dev.yml` -- entre el step `terraform apply` (línea ~119) y `npm run build` (línea ~156): nuevo step que lee `terraform output -raw container_app_fqdn` y sobreescribe `web/src/environments/environment.ts` con `apiBaseUrl: 'https://<fqdn>'`.

## Tasks & Acceptance

**Execution:**
- [x] `src/Api/Program.cs` -- `AddCors`/`UseCors` + `MapGet("/health", ...)` -- endpoint real que prueba la cadena end-to-end
- [x] `src/Api/appsettings.Development.json` -- origen CORS de `ng serve` -- desarrollo local sin depender de un deploy
- [x] `infra/terraform/main.tf` -- env `Cors__AllowedOrigins__0` + `liveness_probe`/`readiness_probe` -- API y SWA se autorizan mutuamente; probes cierran el gap diferido de la historia 1.4
- [x] `web/src/environments/environment.ts` -- archivo nuevo con default local -- fuente única del API base URL en Angular
- [x] `web/src/app/app.config.ts` -- `provideHttpClient()` -- habilita llamadas HTTP desde el shell
- [x] `web/src/app/app.ts` / `app.html` -- consumir `/health` y mostrar el estado -- prueba visual de la cadena end-to-end
- [x] `.github/workflows/cd-dev.yml` -- step de inyección del FQDN real antes del build -- Angular desplegado apunta al Container App real de ese apply

**Acceptance Criteria:**
- Given el esqueleto desplegado en dev (historia 1.4), when se hace `GET /health` contra el FQDN del Container App, then responde 200 con un JSON de estado.
- Given ese mismo despliegue, when se visita el sitio de la SWA, then el shell de Angular llama a `/health` vía CORS y muestra visualmente el estado (éxito o error), confirmando la cadena código→CI→Terraform→Azure→app corriendo.
- Given el Container App desplegado, when Azure evalúa sus probes, then liveness y readiness apuntan a `/health:8080` y lo reportan sano.
- Given un origen no listado en la allowlist de CORS, when llama a la API, then el navegador bloquea la respuesta (sin `AllowAnyOrigin` en el ambiente desplegado).

### Review Findings

_Segunda pasada de code review (2026-09-03) -- blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor._

- [x] [Review][Patch] Sin test que ejerza el timeout de 15s del health-check en Angular -- borrar `.pipe(timeout(HEALTH_CHECK_TIMEOUT_MS))` no haría fallar ningún test existente [web/src/app/app.ts:38-39] -- fix: nuevo test en `app.spec.ts` con fake timers de vitest (app zoneless, sin `fakeAsync`/`tick`)
- [x] [Review][Patch] Test de CORS por env var (`Health_AllowedOrigin_ViaDoubleUnderscoreEnvVar_ReturnsCorsHeader`) muta `Cors__AllowedOrigins__0` a nivel de proceso sin aislar la clase de colisiones con futuros tests que construyan `WebApplicationFactory<Program>` en paralelo [tests/Api.Tests/HealthEndpointCorsTests.cs:83-114] -- fix: `[assembly: CollectionBehavior(DisableTestParallelization = true)]`
- [x] [Review][Patch] La inyección del FQDN real en `environment.ts` (step nuevo de `cd-dev.yml`) no tiene ninguna verificación post-build de que el reemplazo realmente tomó efecto -- si el step se saltara o reordenara después de `npm run build`, el sitio desplegado apuntaría a `localhost` sin que nada en el pipeline lo detecte [.github/workflows/cd-dev.yml:148-161] -- fix: nuevo step "Verify FQDN injection took effect in build output"
- [x] [Review][Patch] Sin test que cubra la rama de log cuando `Cors:AllowedOrigins` llega vacío -- borrar el bloque `if (corsAllowedOrigins.Length == 0) { ... }` no haría fallar ningún test existente [src/Api/Program.cs:30-36] -- fix: nuevo test `Health_EmptyCorsAllowedOrigins_LogsError` con un `ILoggerProvider` de captura
- [x] [Review][Defer] `/health` sigue respondiendo `200 healthy` aunque `Cors:AllowedOrigins` haya llegado vacío -- la única señal de ese fallo es un `LogError`, no el propio endpoint [src/Api/Program.cs:30-36, 51-55] -- deferred, pre-existing
- [x] [Review][Defer] El smoke check de `cd-dev.yml` (paso pre-existente de la historia 1.4) sigue verificando la raíz del Container App, no `/health`, y no envía `Origin` -- no valida el endpoint ni el wiring de CORS que esta historia agrega [.github/workflows/cd-dev.yml:208-236] -- deferred, pre-existing
- [x] [Review][Defer] `src/Api/appsettings.json` (base) no tiene una sección `Cors` como placeholder documentado -- la única forma de descubrir la config es leyendo `appsettings.Development.json` [src/Api/appsettings.json] -- deferred, pre-existing
- [x] [Review][Defer] La suscripción HTTP del health-check en el constructor de `App` no captura `Subscription` ni usa `takeUntilDestroyed()` -- inofensivo hoy porque `App` es el componente raíz y vive toda la vida de la app, pero es un patrón de fuga si se reutiliza en un componente que se destruye [web/src/app/app.ts:38-48] -- deferred, pre-existing
- [x] [Review][Defer] `liveness_probe` y `readiness_probe` son idénticos (mismo path/puerto), sin distinguir "proceso vivo" de "listo para tráfico" -- relevante si una futura historia agrega una dependencia real (ej. Postgres) al chequeo [infra/terraform/main.tf:269-279] -- deferred, pre-existing
- [x] [Review][Defer] `Cors:AllowedOrigins` con un valor no vacío pero mal formado (falta el esquema, slash final) no se valida -- el chequeo actual solo cubre el caso `Length == 0` [src/Api/Program.cs:12,18] -- deferred, pre-existing

## Design Notes

**Por qué CORS y no linked backend:** verificado en la documentación actual del provider `azurerm` (vía Context7) que `azurerm_static_web_app_function_app_registration` solo soporta Function Apps -- no existe un recurso nativo para enlazar un Container App como backend de una Static Web App. Lograrlo requeriría el provider `azapi` (recursos REST crudos sin validación de schema), una superficie nueva y mayor riesgo para lo que sigue siendo un scaffold. CORS es el patrón estándar, ya soportado por `azurerm`/ASP.NET Core sin dependencias nuevas.

**Por qué el FQDN se inyecta en el workflow y no vía `fileReplacements` de Angular:** el patrón estándar de Angular CLI (`environment.ts`/`environment.prod.ts` + `fileReplacements`) asume que el valor de producción se conoce al momento de escribir el código. Aquí el FQDN del Container App lo asigna Azure dinámicamente al hacer `terraform apply` (incluye un sufijo aleatorio del Container App Environment) -- no existe hasta que ese apply corre dentro del mismo workflow, así que se sobreescribe justo antes del build en vez de commitearse.

## Verification

**Commands:**
- `dotnet build Auto.slnx` / `dotnet test Auto.slnx` -- expected: compila y los tests existentes siguen pasando
- `terraform validate` / `terraform fmt -check -recursive` (desde `infra/terraform/`) -- expected: sin errores
- Tras un disparo real de `cd-dev.yml`: `curl https://<container-app-fqdn>/health` -- expected: 200 con JSON de estado

**Matrix Test Audit:**
- Filas 1 y 2 del I/O & Edge-Case Matrix (headers CORS presentes para el origen permitido / ausentes para uno no listado) -- resuelto: `tests/Api.Tests/HealthEndpointCorsTests.cs`, tests de integración con `WebApplicationFactory<Program>` (paquete `Microsoft.AspNetCore.Mvc.Testing` agregado a `Api.Tests.csproj`), corren dentro de `dotnet test Auto.slnx` / CI. Verificado que ambos tests fallan si se rompe la config CORS (sanity check manual, revertido) -- no son vacuos.
- Fila 3 (cold-start del Container App / estado de carga en Angular) sigue cubierta solo por los tests unitarios de `app.spec.ts` (loading/ok/error signals) + verificación manual; no requiere un test de integración contra Azure real para este spec.

**Manual checks (if no CLI):**
- Visitar el sitio de la Static Web App desplegado y confirmar que el estado de `/health` se muestra visualmente (no un error de CORS en la consola del navegador).

## Suggested Review Order

**Endpoint `/health` y CORS (API)**

- Entry point: el endpoint real que prueba la cadena completa -- sin auth, respuesta simple, sin envelope RFC 7807.
  [`Program.cs:51`](../../src/Api/Program.cs#L51)

- CORS restringido a orígenes conocidos vía configuración -- nunca `AllowAnyOrigin` en el ambiente desplegado.
  [`Program.cs:12`](../../src/Api/Program.cs#L12)

- Falla fuerte en los logs del Container App si la config CORS llega vacía, en vez de manifestarse solo como error de CORS en el navegador de quien lo prueba.
  [`Program.cs:30`](../../src/Api/Program.cs#L30)

**CORS y probes (Terraform)**

- Origen real de la Static Web App inyectado al Container App vía variable de entorno -- conocido recién en este `apply`.
  [`main.tf:81`](../../infra/terraform/main.tf#L81)

- `liveness_probe`/`readiness_probe` contra `/health:8080` -- cierra el gap diferido explícitamente por la historia 1.4.
  [`main.tf:88`](../../infra/terraform/main.tf#L88)

**Shell de Angular consumiendo `/health`**

- Llamada HTTP con timeout explícito y manejo de error -- nunca un loading infinito ni una pantalla en blanco durante cold-start.
  [`app.ts:38`](../../web/src/app/app.ts#L38)

- Estado inicial `'loading'` -- el shell nunca arranca en blanco.
  [`app.ts:30`](../../web/src/app/app.ts#L30)

- Template: loading/ok/error, anunciado a lectores de pantalla vía `aria-live`.
  [`app.html:3`](../../web/src/app/app.html#L3)

**Inyección del FQDN real antes del build (CD)**

- El FQDN no existe hasta `terraform apply` -- se sobreescribe justo antes de `npm run build`, nunca se commitea el valor real.
  [`cd-dev.yml:148`](../../.github/workflows/cd-dev.yml#L148)

**Tests**

- Contrato real del body de `/health` (`status`/`timestamp`) -- verificado contra la respuesta real, nunca inferido de un mock.
  [`HealthEndpointCorsTests.cs:58`](../../tests/Api.Tests/HealthEndpointCorsTests.cs#L58)

- Binding real de producción (variable de entorno con doble guion bajo) -- no solo el path de `appsettings.Development.json`.
  [`HealthEndpointCorsTests.cs:83`](../../tests/Api.Tests/HealthEndpointCorsTests.cs#L83)

- CORS allow/deny por origen -- headers presentes/ausentes según la allowlist.
  [`HealthEndpointCorsTests.cs:29`](../../tests/Api.Tests/HealthEndpointCorsTests.cs#L29)

- Estados loading/ok/error del shell de Angular.
  [`app.spec.ts:40`](../../web/src/app/app.spec.ts#L40)

**Periféricos**

- `environment.ts` nuevo -- default local, sobreescrito en CI antes del build de producción.
  [`environment.ts:6`](../../web/src/environments/environment.ts#L6)

- `provideHttpClient()` habilita llamadas HTTP desde el shell.
  [`app.config.ts:7`](../../web/src/app/app.config.ts#L7)

- Origen de `ng serve` para desarrollo local sin necesidad de desplegar.
  [`appsettings.Development.json:9`](../../src/Api/appsettings.Development.json#L9)

- Paquete `Microsoft.AspNetCore.Mvc.Testing` agregado para los tests de integración.
  [`Api.Tests.csproj:8`](../../tests/Api.Tests/Api.Tests.csproj#L8)
