---
title: 'Monitoreo básico de disponibilidad'
type: 'feature'
created: '2026-09-09'
status: 'done'
review_loop_iteration: 1
baseline_commit: 'b6ee12067e917452c98a96804f9c0294980f5072'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-auto-2026-08-19/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El scaffold no tiene ningún monitoreo de disponibilidad conectado -- no existe `azurerm_application_insights` ni ninguna alerta en Terraform, así que una caída de la API o del frontend solo se descubre por reportes de usuarios, sin forma de verificar el cumplimiento de NFR-4 (99% uptime).

**Approach:** Provisionar Application Insights (workspace-based, sobre el Log Analytics Workspace ya existente) vía Terraform; instrumentar la API con `Azure.Monitor.OpenTelemetry.AspNetCore` para métricas de latencia/errores automáticas; medir disponibilidad de API y frontend con Standard (URL ping) Web Tests contra `/health` y la Static Web App; y una alerta de métrica sobre el resultado de disponibilidad que notifica vía un Action Group.

## Boundaries & Constraints

**Always:**
- Application Insights en modo workspace-based, apuntando al `azurerm_log_analytics_workspace.main` ya existente -- no crear un segundo workspace.
- La connection string de App Insights se pasa a la API vía la variable de entorno estándar `APPLICATIONINSIGHTS_CONNECTION_STRING` en `azurerm_container_app.api` (mismo patrón que `Cors__AllowedOrigins__0`, `main.tf:77-83`), nunca hardcodeada.
- El monitoreo de disponibilidad del frontend se hace vía Standard Web Test (ping HTTP) contra la URL pública de la Static Web App -- no se agrega ningún SDK de telemetría en Angular.
- Nombres de recursos nuevos siguen la convención de `locals.tf`: `appi-auto-${var.environment}` (App Insights), `ag-auto-${var.environment}` (Action Group), `alert-availability-auto-${var.environment}` (regla de alerta).
- Todo recurso nuevo se destruye limpiamente con el resto de `rg-auto-dev` -- sin `prevent_destroy`.

**Ask First:** Destinatario (email) del Action Group que recibe la notificación de la alerta -- no hay ningún canal de notificación operativo precedente en el repo (Communication Services existente es para OTP de usuarios, Epic 5, no para alertas).

**Never:**
- No agrega SDK de telemetría cliente-side en Angular (`@microsoft/applicationinsights-web` u otro) -- el repo no tiene mecanismo de entornos nativo en Angular ni precedente de cómo tratar un valor embebido en el bundle JS público; fuera de alcance.
- No crea dashboards, política de retención de logs, ni APM/tracing distribuido -- explícitamente diferido en `ARCHITECTURE-SPINE.md:270`.
- No condiciona la alerta a `var.environment == "prod"` -- el mismo Terraform aplica igual a cualquier ambiente desplegado (hoy solo existe `dev`).
- No usa el SDK clásico `Microsoft.ApplicationInsights.AspNetCore` -- usa el distro moderno de OpenTelemetry (el clásico está en mantenimiento).
- No modifica `ci.yml`, `cd-dev.yml`, ni `destroy-dev.yml`.

## Code Map

- `infra/terraform/main.tf:34-41` -- `azurerm_log_analytics_workspace.main` ya existente, reusar su id como `workspace_id` del nuevo `azurerm_application_insights`.
- `infra/terraform/main.tf:55-112` -- `azurerm_container_app.api`, agregar `env` block `APPLICATIONINSIGHTS_CONNECTION_STRING` (mismo patrón que `Cors__AllowedOrigins__0`, líneas 77-83).
- `infra/terraform/main.tf:114-121` -- `azurerm_static_web_app.main`, su `default_host_name` es el target del web test de frontend.
- `infra/terraform/locals.tf` -- agregar nombres derivados para los recursos nuevos, mismo patrón `<prefijo>-${var.project_prefix}-${var.environment}`.
- **`azurerm_monitor_metric_alert` -- usar el bloque de criteria dedicado `application_insights_web_test_location_availability_criteria` (provider `azurerm`), NO un `criteria` genérico sobre la métrica agregada `availabilityResults/availabilityPercentage`.** Ese bloque genérico promedia el resultado de TODOS los web tests del componente App Insights -- una caída total de un target puede quedar diluida por el otro target sano y nunca cruzar el umbral (hallazgo de revisión, ver Spec Change Log). El bloque dedicado toma `web_test_id` + `component_id` y cuenta ubicaciones geográficas fallidas por test individual -- requiere **una regla de alerta por Standard Web Test** (dos en total), cada una con `scopes = [azurerm_application_insights.main.id, azurerm_application_insights_standard_web_test.<X>.id]`.
- `infra/terraform/variables.tf` -- `availability_alert_email`: agregar `sensitive = true` (evita que el email quede en texto plano en logs de plan/apply) y `default = "antonio.tamez.s@gmail.com"` (resuelto vía Ask First con el humano) -- sigue sin tocar `cd-dev.yml`.
- `src/Api/Api.csproj:22-26` -- agregar `PackageReference Azure.Monitor.OpenTelemetry.AspNetCore` (última estable verificada en NuGet al implementar, no asumir un número de versión fijo).
- `src/Api/Program.cs` -- agregar el registro de OpenTelemetry condicional a `APPLICATIONINSIGHTS_CONNECTION_STRING` (ver Spec Change Log, esa parte ya funciona y se mantiene). Envolver la llamada a `AddOpenTelemetry().UseAzureMonitor()` en `try/catch`: si la variable viene seteada pero con formato inválido, `UseAzureMonitor()` puede lanzar una excepción al arrancar el host (mismo mecanismo eager ya documentado) -- capturarla, escribir un aviso a `Console.Error` y continuar sin telemetría en vez de tumbar el proceso.
- `tests/Api.Tests/HealthEndpointCorsTests.cs` -- patrón a imitar: `WebApplicationFactory<Program>` + `Environment.SetEnvironmentVariable` + `CapturingLoggerProvider`, para el nuevo test de wiring. El comentario XML del test nuevo debe describir el registro como **condicional** (no "incondicional" -- error detectado en la ronda de revisión anterior).

## Tasks & Acceptance

**Execution:**
- [x] `infra/terraform/main.tf` -- agregar `azurerm_application_insights.main` (workspace-based, `workspace_id = azurerm_log_analytics_workspace.main.id`) -- cumple "provisionado vía Terraform"
- [x] `infra/terraform/main.tf` -- agregar `env` block `APPLICATIONINSIGHTS_CONNECTION_STRING = azurerm_application_insights.main.connection_string` al `azurerm_container_app.api` -- conecta la API sin tocar CI/CD
- [x] `infra/terraform/main.tf` -- agregar `azurerm_application_insights_standard_web_test` para `/health` (API) y para la URL raíz de la Static Web App (frontend) -- cumple "conectado ... al frontend" sin SDK cliente
- [x] `infra/terraform/main.tf` -- agregar `azurerm_monitor_action_group` (`email_receiver` = `var.availability_alert_email`)
- [x] `infra/terraform/main.tf` -- agregar **dos** `azurerm_monitor_metric_alert` (uno por Standard Web Test), cada uno con un bloque `application_insights_web_test_location_availability_criteria` (`web_test_id`, `component_id`, `failed_location_count = 2` -- de los 2 `geo_locations` configurados, ambos deben reportar falla antes de notificar, para evitar falsos positivos por un blip regional aislado) -- cumple "alerta básica" sin que un target sano enmascare la caída del otro (ver Spec Change Log)
- [x] `infra/terraform/locals.tf` -- agregar nombres derivados para los recursos nuevos, siguiendo el patrón existente (dos nombres de alerta, uno por web test, en vez de uno solo)
- [x] `infra/terraform/variables.tf` -- `availability_alert_email` con `sensitive = true` y `default = "antonio.tamez.s@gmail.com"`
- [x] `src/Api/Api.csproj` -- agregar `PackageReference Azure.Monitor.OpenTelemetry.AspNetCore`
- [x] `src/Api/Program.cs` -- agregar el registro de OpenTelemetry condicional a `APPLICATIONINSIGHTS_CONNECTION_STRING` -- [Deviation] el `try/catch` solo no basta (ver Spec Change Log): se agregó además una validación estructural previa (`IsWellFormedConnectionString`) porque `UseAzureMonitor()` no valida el formato de forma síncrona
- [x] `tests/Api.Tests/ApplicationInsightsWiringTests.cs` -- nuevo test (patrón `HealthEndpointCorsTests.cs`): con `APPLICATIONINSIGHTS_CONNECTION_STRING` seteada, el servicio de OpenTelemetry se registra en el contenedor DI; sin la variable, la app sigue arrancando sin excepción; comentario XML describe el registro como condicional

**Acceptance Criteria:**
- Given el ambiente `dev` desplegado (Historia 1.4), when se aplica el Terraform de esta historia, then existe un recurso `azurerm_application_insights` workspace-based conectado al Log Analytics ya existente.
- Given la API corriendo con `APPLICATIONINSIGHTS_CONNECTION_STRING` seteada, when se recibe una petición a cualquier endpoint, then Application Insights registra latencia y resultado (éxito/error) de esa request.
- Given los Standard Web Tests configurados contra `/health` y la Static Web App, when cualquiera de los dos deja de responder, then Application Insights registra la caída como resultado de disponibilidad fallido.
- Given uno de los dos targets (API o frontend) cae por debajo del umbral de NFR-4 (99%) mientras el otro sigue disponible, when se evalúa su regla de alerta correspondiente, then el Action Group recibe una notificación de ESE target específico, sin que la disponibilidad del otro lo enmascare.

## Design Notes

Se usa `azurerm_application_insights_standard_web_test` (ping HTTP) en vez de un SDK cliente en Angular porque el repo no tiene ningún mecanismo de entorno nativo en Angular (`angular.json` sin `fileReplacements`, solo un `environment.ts`) ni precedente de cómo tratar un valor embebido en un bundle JS público -- un ping test mide la disponibilidad real del sitio servido sin tocar el código Angular ni el pipeline de CI/CD existente. Se usa `Azure.Monitor.OpenTelemetry.AspNetCore` (no el SDK clásico) porque es el enfoque activamente mantenido por Microsoft y su wiring es una sola línea que lee la connection string de la variable de entorno estándar, sin requerir cambios en `appsettings.json`.

Dos reglas de alerta (una por Standard Web Test) en vez de una sola sobre la métrica agregada del componente: `application_insights_web_test_location_availability_criteria` es el bloque que el provider `azurerm` ofrece específicamente para alertar sobre un web test individual, y es la única forma de que la caída total de un target (ej. la API) no quede promediada/enmascarada por el otro target (el frontend) siguiendo sano. `failed_location_count = 2` (de 2 `geo_locations`) exige que ambas ubicaciones reporten falla antes de notificar -- evita que un blip transitorio de una sola región dispare una alerta de "básico" nivel; no hay ningún requisito de NFR-4 sobre sensibilidad regional que justifique un umbral de 1.

## Spec Change Log

- **2026-09-09** -- Finding (verification-gap, empíricamente reproducido: `dotnet test Auto.slnx` falló al construir `src/Api` con `System.InvalidOperationException: A connection string was not found. Please set your connection string.`): la primera implementación llamaba `builder.Services.AddOpenTelemetry().UseAzureMonitor();` incondicionalmente, tal como sugiere el Code Map. Esto no falla al registrar el servicio en el contenedor DI, sino más tarde, al arrancar el host (`Host.StartAsync` crea el `MeterProvider` de forma eager vía un hosted service de OpenTelemetry) -- sin `APPLICATIONINSIGHTS_CONNECTION_STRING` seteada, cualquier arranque real del host lanza esa excepción. Esto rompía tres cosas sin que el Tasks & Acceptance original lo anticipara: la generación build-time del contrato OpenAPI (spec 1.7, que corre la app dentro del build para extraer el documento), cualquier `dotnet run` local sin Application Insights desplegado, y el propio test de wiring nuevo de esta historia en su variante "sin la variable". Amended: `Program.cs` ahora lee `APPLICATIONINSIGHTS_CONNECTION_STRING` vía `builder.Configuration` (mismo mecanismo estándar, ninguna variable nueva) y solo llama `AddOpenTelemetry().UseAzureMonitor()` si viene no vacía. Avoids: que agregar monitoreo básico rompa el build de CI y el arranque local para cualquier desarrollador sin credenciales de Azure Monitor. KEEP: el resto del wiring (paquete, versión `1.6.0` -- última estable en NuGet al momento de esta historia, no `1.4.0` como se estimó inicialmente --, distro moderno de OpenTelemetry, sin tocar `appsettings.json`) sigue vigente sin cambios; la Acceptance Criteria de esta historia ("cuando se recibe una petición ... Application Insights registra latencia y resultado") sigue cumpliéndose igual en el ambiente desplegado, donde Terraform siempre inyecta la variable.

- **2026-09-10, review_loop_iteration 1** -- Finding (bad_spec, hallado por revisión `blind-hunter`: el Code Map original solo pedía "agregar `azurerm_monitor_metric_alert` sobre `availabilityResults/availabilityPercentage` con umbral 99%", sin especificar granularidad por target): la primera implementación creó UNA sola regla de alerta con un `criteria` genérico sobre la métrica agregada del componente completo de Application Insights -- esa métrica promedia el resultado de AMBOS Standard Web Tests (API y frontend). Una caída total de un target puede quedar diluida por el otro target sano y nunca cruzar el umbral del 99%, dejando la caída sin notificar -- contradice el propósito explícito de la historia ("conectado a la API y al frontend ... alerta que notifica cuando la disponibilidad cae"). Amended: Code Map y Tasks ahora piden DOS `azurerm_monitor_metric_alert` (uno por Standard Web Test), cada uno con el bloque dedicado `application_insights_web_test_location_availability_criteria` (`web_test_id`, `component_id`, `failed_location_count = 2`) en vez del `criteria` genérico -- ver Design Notes para el razonamiento de `failed_location_count`. La Acceptance Criteria de "alerta básica" se reescribió para exigir explícitamente que la caída de un target no quede enmascarada por el otro. De paso, se resolvió aquí el "Ask First" pendiente (destinatario del Action Group, decidido por el humano: `antonio.tamez.s@gmail.com`) y se plegaron tres hallazgos menores de la misma ronda de revisión que habrían sido `patch` si no hubiera loopback: `availability_alert_email` ahora lleva `sensitive = true` y un `default` (evita tener que tocar `cd-dev.yml`, respetando el `Never` ya vigente); el registro de OpenTelemetry en `Program.cs` se envuelve en `try/catch` (una connection string presente pero mal formada no debe tumbar el host, mismo mecanismo eager ya documentado arriba); y el comentario XML del test de wiring debe describir el registro como condicional, no incondicional (contradecía la implementación real). Avoids: que la historia 1.8 quede "implementada" pero silenciosamente incapaz de cumplir su propio objetivo (avisar cuando algo se cae) en el caso más simple de caída parcial. KEEP: la Application Insights workspace-based, el `env` block de connection string en el Container App, ambos Standard Web Test (ping HTTP, sin SDK cliente en Angular), el distro moderno de OpenTelemetry con el guard condicional a la variable de entorno, y el patrón de test (`WebApplicationFactory` + `HealthEndpointCorsTests` como referencia) -- todo eso sigue vigente sin cambios, solo se corrige la granularidad de la alerta y se resuelven los tres hallazgos menores.

- **2026-09-10** -- Deviation (encontrada al re-implementar, verificado empíricamente corriendo el `Api.dll` real con una connection string mal formada): el `try/catch` que el Code Map pedía envolver alrededor de `AddOpenTelemetry().UseAzureMonitor()` no alcanza -- `UseAzureMonitor()` no valida el formato de la connection string de forma síncrona; el parseo real ocurre después, dentro de `Host.StartAsync` (durante `app.Run()`), fuera del alcance de ese `try/catch`. Con solo el `try/catch` literal, una connection string presente pero mal formada seguía tumbando el proceso real antes de que Kestrel llegara a escuchar. Amended: se agregó `IsWellFormedConnectionString` (validación estructural mínima `Key=Value;Key2=Value2...`, sin depender de tipos internos del SDK de Azure) como pre-chequeo antes de llamar a `UseAzureMonitor()`, dejando el `try/catch` como defensa adicional. Avoids: que un valor mal formado de `APPLICATIONINSIGHTS_CONNECTION_STRING` tumbe el arranque real de la API, violando el AC de esta historia ("sin la variable, o con un valor inválido, la app sigue arrancando sin excepción"). KEEP: todo lo demás de esta ronda (dos `azurerm_monitor_metric_alert` con `application_insights_web_test_location_availability_criteria`, `availability_alert_email` con `sensitive`+`default`) sigue vigente sin cambios -- verificado con `terraform validate`, un `terraform plan -var environment=dev` real contra el backend de Azure (16 creates esperados, 0 destroys, sin pedir variables), y `dotnet test Auto.slnx` (12/12).

## Verification

**Commands:**
- `terraform -chdir=infra/terraform validate` -- expected: sin errores de sintaxis/tipos en los recursos nuevos.
- `terraform -chdir=infra/terraform plan -var environment=dev` -- expected: el plan muestra creación de `azurerm_application_insights`, 2x `azurerm_application_insights_standard_web_test`, `azurerm_monitor_action_group`, **2x** `azurerm_monitor_metric_alert` (uno por web test, cada uno con `application_insights_web_test_location_availability_criteria`, no con un `criteria` genérico sobre la métrica agregada), y un `env` block nuevo en el Container App -- sin destruir ningún recurso existente, sin pedir `-var availability_alert_email` (tiene default).
- `dotnet test Auto.slnx` -- expected: pasa el nuevo test de wiring de OpenTelemetry junto con el resto de la suite.

**Manual checks (if no CLI):**
- Tras un `apply` real a dev: abrir Application Insights en Azure Portal y confirmar que llegan resultados de "Availability" para ambos web tests y al menos una request registrada tras pegarle a `/health`.

## Suggested Review Order

- Application Insights workspace-based, reusa el Log Analytics ya existente en vez de crear uno segundo.
  [`main.tf:46`](../../infra/terraform/main.tf#L46)

**Alerta por-target, no agregada (el fix del primer ciclo de revisión)**

- Regla de alerta dedicada a la API, con el criterio de disponibilidad de web test individual, no la métrica agregada del componente.
  [`main.tf:198`](../../infra/terraform/main.tf#L198)

- Mismo patrón para el frontend -- dos reglas independientes evitan que un target sano enmascare la caída del otro.
  [`main.tf:220`](../../infra/terraform/main.tf#L220)

- `failed_location_count` derivado de la lista de `geo_locations`, no un literal hardcodeado (fix del segundo ciclo de revisión).
  [`main.tf:210`](../../infra/terraform/main.tf#L210)

**Conexión de la API a Application Insights sin tumbar el arranque**

- Connection string inyectada por Terraform, mismo patrón que el CORS origin ya existente.
  [`main.tf:102`](../../infra/terraform/main.tf#L102)

- Registro de OpenTelemetry condicional a que la variable venga seteada -- sin esto, `Host.StartAsync` lanza una excepción eager (ver Spec Change Log).
  [`Program.cs:37`](../../src/Api/Program.cs#L37)

- Validación estructural adicional (forma `Key=Value` + key reconocida) antes de registrar -- un `try/catch` alrededor de la llamada no alcanza porque el parseo real ocurre después, en `Host.StartAsync`.
  [`Program.cs:121`](../../src/Api/Program.cs#L121)

**Disponibilidad del frontend sin SDK cliente en Angular**

- Standard Web Test (ping HTTP) contra la API y contra la Static Web App -- mide disponibilidad real sin instrumentar Angular.
  [`main.tf:147`](../../infra/terraform/main.tf#L147)

**Notificación**

- Action Group con el email resuelto vía Ask First con el humano.
  [`main.tf:181`](../../infra/terraform/main.tf#L181)

- Validación de formato de email, mismo patrón que la variable hermana `api_container_image` (fix del segundo ciclo de revisión).
  [`variables.tf:73`](../../infra/terraform/variables.tf#L73)

**Peripherals**

- Paquete NuGet del distro moderno de OpenTelemetry, no el SDK clásico.
  [`Api.csproj:38`](../../src/Api/Api.csproj#L38)

- Cubre los tres casos del guard condicional: connection string válida, ausente, y con forma inválida o sin key reconocida.
  [`ApplicationInsightsWiringTests.cs`](../../tests/Api.Tests/ApplicationInsightsWiringTests.cs)
