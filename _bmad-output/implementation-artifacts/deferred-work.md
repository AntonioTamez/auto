- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-esqueleto-del-repositorio-clean-architecture-angular.md`
  summary: Decidir explícitamente si `src/Api` necesita una referencia directa a `src/Infrastructure` para poder componer sus implementaciones concretas en el contenedor de DI.
  evidence: Hoy `Api.csproj` solo referencia `Application` (tal como definía el Code Map de la historia 1.1). Como `Infrastructure` todavía está vacío, esto no rompe nada aún, pero el propio Architecture Spine dice que `Api` "compone DI e integra Infrastructure" sin declarar explícitamente esa dependencia de proyecto. Revisar en cuanto `Infrastructure` tenga su primera implementación real (EF Core, repos, etc.).

- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-esqueleto-del-repositorio-clean-architecture-angular.md`
  summary: `Microsoft.AspNetCore.OpenApi 10.0.9` arrastra `Microsoft.OpenApi 2.0.0`, marcado por NuGet como advisory de severidad alta (NU1903, GHSA-v5pm-xwqc-g5wc).
  evidence: `dotnet build` sobre `src/Api` compila con 0 errores pero 2 warnings por este advisory. Es el default del template `dotnet new webapi`, no una elección de esta historia. Hoy no hay superficie de ataque real (no se expone ningún schema OpenAPI todavía), pero conviene resolverlo (actualizar/pinnear versión) antes de que la historia 1.7 publique el contrato OpenAPI real.

## Deferred from: code review of story-1-1 (2026-08-20)

- Sin `.gitattributes`; `.editorconfig` fija `end_of_line = crlf` para todo el repo sin política definida para runners Linux en CI (`.editorconfig:8`). Revisar al configurar la historia 1.3 (CI).
- RFC 7807 Problem Details no está configurado en `Program.cs`, aunque `epic-1-context.md` lo señala como decisión a nivel de scaffold (`src/Api/Program.cs:1-17`). No hay endpoints todavía; revisar cuando se agregue el primer endpoint real.
- No hay middleware de validación JWT centralizado, y no estaba registrado como diferido pese a que `epic-1-context.md` lo llama "decisión de nivel scaffold" (`src/Api/Program.cs:1-17`). Revisar cuando exista el primer endpoint autenticado (Epic 5).
- No existe ningún proyecto de tests .NET (`tests/Domain.Tests`, etc.). La historia 1.3 (CI build+test) necesitará algo que ejecutar del lado backend.
- `.gitignore` raíz no cubre patrones de secretos/env (`.env`, connection strings, etc.) más allá de lo exigido explícitamente por el spec de la historia 1.1 (`.gitignore:1-23`). Revisar cuando se agreguen credenciales reales (Postgres, Blob Storage, Communication Services).
- BOM UTF-8 inconsistente entre archivos generados: `Domain.csproj`, `Application.csproj`, `Infrastructure.csproj` y `launchSettings.json` lo tienen; `Api.csproj` y `Program.cs` no (`src/Domain/Domain.csproj:1`). Cosmético, sin impacto funcional conocido hoy.
- `Program.cs` usa `UseHttpsRedirection()` sin `UseForwardedHeaders` (`src/Api/Program.cs:15`); provocará un redirect-loop una vez desplegado detrás del reverse proxy de Azure Container Apps. Revisar en la historia 1.4 (CD a dev).
- `global.json` fija `rollForward: latestFeature` (`global.json:3-4`); puede fallar en máquinas de CI que no tengan exactamente el feature band 10.0.3xx del SDK. Revisar al configurar los runners de la historia 1.3.

## Deferred from: build workflow of story-1-2 (2026-08-20)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-2-infraestructura-azure-definida-en-terraform.md`
  summary: `project_prefix` (default `auto` en `infra/terraform/variables.tf`) debe actualizarse al nombre definitivo del producto antes del primer `terraform apply` real (historia 1.4).
  evidence: Confirmado explícitamente por el humano el 2026-08-20 — el nombre final de la app aún no está decidido (ver también Deferred "Product naming" en `ARCHITECTURE-SPINE.md`). Cambiar el default después de un `apply` real requeriría recrear recursos con nombre globalmente único (Storage Account, PostgreSQL), no solo renombrarlos.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-2-infraestructura-azure-definida-en-terraform.md`
  summary: `azurerm_communication_service.data_location` está fijo en `"United States"` porque Azure Communication Services no ofrece `data_location = "Mexico"` — un gap real de residencia de datos frente a la elección de región `mexicocentral` para el resto de los recursos.
  evidence: Descubierto durante la implementación (`infra/terraform/main.tf:68`); ACS solo acepta un conjunto fijo de valores de `data_location` y México no está entre ellos. Revisar con quien posea el tema de cumplimiento antes de que la historia que dispare OTP por email (Epic 5) dependa de este recurso en producción.

## Deferred from: code review of story-1-2 (2026-08-20)

- `azurerm_postgresql_flexible_server.main` no tiene ninguna regla de firewall/red configurada (`infra/terraform/main.tf`) — tal como está, nada (ni siquiera el futuro Container App) tiene una vía definida para alcanzar la base de datos. Revisar en la historia 1.4 (CD) cuando la API necesite conectarse realmente a Postgres.
- No se crea ningún `azurerm_storage_container` dentro de `azurerm_storage_account.main` (`infra/terraform/main.tf`) — solo existe la cuenta, no un contenedor con el nivel de acceso adecuado para imágenes de vehículos. Revisar cuando la app necesite escribir/leer blobs reales.
- `azurerm_communication_service.main` solo crea el servicio base; falta un Email Communication Service vinculado y un dominio de remitente verificado, que es lo que realmente se necesita para enviar los emails de OTP (AD-4). Revisar en la Epic 5 (cuentas/OTP).
- No hay estrategia de identidad administrada definida para que el futuro Container App se autentique contra Storage/PostgreSQL/Communication Service — todo apunta hoy a connection strings/keys (ver el output sensible de password de Postgres) en vez de acceso sin contraseña vía `azurerm_user_assigned_identity` + RBAC. Revisar en la historia 1.4 (CD) al definir cómo la API real consume estos recursos.

## Deferred from: code review of story-1-2 (2026-08-20, segunda pasada)

- Sin estrategia de gestión de secretos (Key Vault u otro) para el password admin de Postgres (`infra/terraform/main.tf:15`) — hoy solo existe como `random_password` en el state de Terraform y como output `sensitive`, sin plan de almacenamiento/rotación. Distinto del gap de identidad administrada ya registrado arriba (ese es sobre cómo se autentica la app; este es sobre dónde vive el secreto).
- El patrón de acceso público de `azurerm_storage_account.main` para imágenes de vehículos no está decidido (CORS, `network_rules`, URL pública vs. SAS/CDN) — extiende el gap ya registrado de que aún no existe un `azurerm_storage_container`. Revisar cuando la app necesite servir imágenes realmente al frontend Angular.
- El storage account de bootstrap del state (`stautotfstate`, `infra/terraform/README.md`) no tiene restricción de red (`--default-action Deny` + reglas de IP) pese a alojar el password admin de Postgres en su `.tfstate` — requiere decidir primero qué IPs necesitan acceso (desarrolladores locales, runners de CI de la historia 1.3/1.4) antes de poder restringirlo sin romper el flujo documentado.

## Deferred from: build workflow of story-1-3 (2026-08-24)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-ci-build-y-test-en-cada-push-pr.md`
  summary: Branch protection real en `main` (requerir los status checks `backend`/`frontend` antes de fusionar, AD-17) **bloqueada por el plan de GitHub**, no por falta de configuración — `gh api repos/AntonioTamez/auto/branches/main/protection` devuelve `403 "Upgrade to GitHub Pro or make this repository public to enable this feature"`. El plan Free no ofrece required status checks en repos privados.
  evidence: Intentado el 2026-08-24 después del primer push real (`backend`/`frontend` corrieron en verde, commit `d6c386a`, confirmado vía `gh api repos/AntonioTamez/auto/commits/d6c386a/check-runs`) — el payload ya era correcto (nombres de job confirmados por el humano), pero GitHub rechazó la llamada por el plan de la cuenta, no por un error de configuración. Hoy el pipeline corre y reporta en cada push/PR, pero **no bloquea el merge** — un PR con `backend`/`frontend` en rojo todavía se puede fusionar manualmente. Resolver cuando el humano decida entre actualizar a GitHub Pro o hacer el repo público (ambas opciones fueron explícitamente puestas sobre la mesa y rechazadas por ahora).
  addendum (2026-08-27, code review): el payload documentado en `README.md` § CI fija `enforce_admins=false` y `required_pull_request_reviews=null` — cuando esto se active, un admin del repo aún podría fusionar con el pipeline en rojo (el `null` de reviews es irrelevante para AD-17, que solo exige status checks, no aprobación de código). Decidir la postura de `enforce_admins` junto con la decisión de plan/visibilidad de arriba, no antes.

## Deferred from: code review of story-1-3 (2026-08-24)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-ci-build-y-test-en-cada-push-pr.md`
  summary: Un push a una rama con PR abierto dispara el pipeline dos veces (evento `push` y evento `pull_request` sobre el mismo commit) — el `concurrency` group agregado solo cancela runs superados por un push posterior, no elimina la doble ejecución del mismo commit.
  evidence: `.github/workflows/ci.yml` dispara en `push: branches: ['**']` Y en `pull_request`, tal como lo exige el spec (AC: "push a cualquier rama" y "PR abierto" son escenarios independientes). Eliminarlo del todo requeriría restringir el trigger `push` (p.ej. solo a `main`), lo que rompería el AC de "push a cualquier rama corre el pipeline" para ramas sin PR — es una decisión de diseño, no un bug mecánico.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-ci-build-y-test-en-cada-push-pr.md`
  summary: `.github/workflows/ci.yml` no filtra por `paths`/`paths-ignore`, así que cambios solo de documentación (README, `_bmad-output/**`) igual disparan el build+test completo de ambos stacks.
  evidence: Confirmado por el propio diff de esta historia (editó `README.md` y `deferred-work.md` sin tocar código, y aun así el pipeline correría completo). Definir la lista de paths a ignorar con seguridad (sin excluir accidentalmente algo que sí debería disparar CI) requiere más cuidado del que amerita un patch mecánico.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-ci-build-y-test-en-cada-push-pr.md`
  summary: Las Actions de terceros (`actions/checkout@v7`, `actions/setup-dotnet@v6`, `actions/setup-node@v7`) están fijadas solo a tag de versión mayor, no a SHA de commit — el hardening estándar contra supply-chain tampering las fija por SHA.
  evidence: Hallazgo de code review (blind-hunter). Es una decisión de postura de seguridad a nivel de repo (aplicaría a cualquier workflow futuro, no solo a este), y fijar por SHA sin un mecanismo de actualización (Dependabot/Renovate) crea su propia carga de mantenimiento — mejor tratarlo como política transversal que como patch de esta historia.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-ci-build-y-test-en-cada-push-pr.md`
  summary: Ningún job publica artefactos de diagnóstico en caso de falla (sin `--logger trx` en `dotnet test`, sin `actions/upload-artifact` para logs/resultados) — un run rojo solo deja el log crudo de consola para triage.
  evidence: Hallazgo de code review (blind-hunter). No lo exige ningún AC de esta historia; revisar cuando el pipeline empiece a fallar con frecuencia real y el log de consola no alcance para diagnosticar.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-ci-build-y-test-en-cada-push-pr.md`
  summary: El job `backend` no cachea paquetes NuGet (restore corre por red en cada run), a diferencia del job `frontend` que sí cachea `npm` vía `actions/setup-node`.
  evidence: Hallazgo de code review (blind-hunter) — asimetría de optimización entre ambos jobs. No afecta corrección, solo tiempo de pipeline; revisar si el tiempo de `dotnet restore` se vuelve un cuello de botella real.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-ci-build-y-test-en-cada-push-pr.md`
  summary: Ningún job impone una barra de "sin warnings" (sin `TreatWarningsAsErrors`/`-warnaserror` en .NET, sin paso de lint/`tsc --noEmit` en Angular) — "build y test" tal como quedó implementado permite que warnings del compilador/analizador se fusionen mientras el código compile.
  evidence: Hallazgo de code review (blind-hunter). No estaba en el alcance del AC de esta historia (solo exige compilar y correr pruebas); es una decisión de calidad de código a nivel de repo que amerita su propia conversación con el humano antes de imponerse como gate bloqueante.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-ci-build-y-test-en-cada-push-pr.md`
  summary: `src/Api/Program.cs` usa top-level statements sin el marcador `public partial class Program { }` que necesita `WebApplicationFactory<Program>` para pruebas de integración.
  evidence: Hallazgo de code review (blind-hunter). Hoy no existe ninguna prueba de integración (solo el placeholder unitario de `Api.Tests`); agregar el marcador es trivial pero prematuro sin una prueba real que lo necesite — revisar cuando llegue la primera prueba de integración (candidata natural: historia 1.5, health-check end-to-end).

## Deferred from: code review of story-1-3, segunda pasada (2026-08-27)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-ci-build-y-test-en-cada-push-pr.md`
  summary: El código Terraform de `infra/terraform/` (historia 1.2) no tiene ninguna validación automática en CI (`terraform fmt -check`/`terraform validate`, ni `tflint`) — "build y test en cada push/PR" hoy solo cubre .NET y Angular.
  evidence: Hallazgo de code review (blind-hunter, segunda pasada). No es causado por esta historia -- el código Terraform ya existía sin cobertura de CI desde la historia 1.2, y el AC de la 1.3 solo exige compilar/probar backend y frontend. Revisar cuando el pipeline de CD (historia 1.4) empiece a consumir ese código con más frecuencia, o si un `terraform apply` real se rompe por un error que `validate` habría atrapado.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-ci-build-y-test-en-cada-push-pr.md`
  summary: `concurrency.cancel-in-progress: true` en `ci.yml` cancelará un run en curso sobre `main` si llega un push más nuevo antes de que termine -- una vez que `main` dispare CD real (historia 1.4), un run cancelado se ve igual que un run fallido en la UI de Checks.
  evidence: Hallazgo de code review (blind-hunter, segunda pasada). Hoy `main` no dispara ningún despliegue, así que no hay consecuencia real todavía; revisar la semántica de concurrency (o restringir `cancel-in-progress` a ramas no-`main`) al implementar la historia 1.4.

## Deferred from: build workflow of story-1-4 (2026-08-28)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: `src/Api/Program.cs` sigue usando `UseHttpsRedirection()` sin `UseForwardedHeaders`, tal como ya lo advertía el deferred item de la historia 1.1 ("revisar en la historia 1.4, CD a dev") -- ahora que la API corre detrás del reverse proxy de Azure Container Apps (TLS terminado en el borde, tráfico interno hacia el contenedor sobre HTTP en `target_port 8080`), ese redirect puede apuntar a un esquema/puerto que el cliente no puede alcanzar.
  evidence: No se tocó `Program.cs` en esta historia porque el Code Map del spec no lo incluye (solo Dockerfile, Terraform, workflow, staticwebapp.config.json y README) y modificarlo sería lógica de negocio/composición fuera del alcance declarado ("Never": sin lógica de negocio nueva). El AC "la API responde" se cumple igual (la conexión HTTP llega y el pipeline de middleware responde, aunque sea con un 307 mal dirigido o un 404 -- no hay endpoints mapeados fuera de `IsDevelopment()`), pero no es una verificación funcional fuerte. Revisar junto con la historia 1.5 (health-check), que es la primera que necesita una respuesta 200 real.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: `infra/terraform/README.md` documenta las tablas de Variables y Outputs del módulo, pero no se actualizaron con la nueva variable `api_container_image` ni el nuevo output `container_app_fqdn` agregados por esta historia.
  evidence: Ese archivo no está en el Code Map del spec (que solo lista `main.tf`, `variables.tf`, `outputs.tf`). Las tablas de ese README ahora están desactualizadas respecto al código real del módulo; revisar la próxima vez que se toque `infra/terraform/README.md` o antes de onboardear a alguien nuevo al módulo.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: `azurerm_container_app.api` fija `max_replicas = 1` -- valor no especificado por el spec (que solo fija `min_replicas = 0`) y elegido como default conservador de costo para dev.
  evidence: Decisión de implementación tomada sin confirmación explícita del humano. Si el ambiente dev necesita absorber más de una réplica concurrente (pruebas de carga, demos), revisar este valor.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: `src/Api/Dockerfile` usa tags flotantes (`mcr.microsoft.com/dotnet/sdk:10.0`, `mcr.microsoft.com/dotnet/aspnet:10.0`) en vez de pinnear por digest, y `.github/workflows/cd-dev.yml` fija las GitHub Actions de terceros solo por tag de major (`@v3`/`@v4`/`@v7`/`@v1`), no por SHA de commit.
  evidence: Mismo patrón/decisión de postura ya diferida para `ci.yml` en la historia 1.3 ("Las Actions de terceros... están fijadas solo a tag de versión mayor") -- se mantiene consistencia entre ambos workflows en vez de pinnear solo este. Tratar como política transversal de supply-chain a decidir junto con ese ítem, no como parche aislado de esta historia.

## Deferred from: code review of story-1-4 (2026-08-28)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: `azurerm_container_app.api` no tiene ningún `identity` block ni variables/secrets de conexión a Postgres/Storage/Communication Service -- los gaps ya registrados en la historia 1.2 ("sin estrategia de identidad administrada... revisar en la historia 1.4 (CD)" y "sin regla de firewall/red hacia Postgres... revisar en la historia 1.4") siguen abiertos.
  evidence: Hallazgo de code review (blind-hunter). Explícitamente fuera de alcance por el `Never` congelado del spec de esta historia ("no se conecta la API real a Postgres/Storage/Communication Service"): 1.4 solo prueba que el pipeline despliega el esqueleto, no que la app tenga lógica de negocio. Revisar en la primera historia que necesite que la API lea/escriba datos reales (Epic 2 en adelante).

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: El GitHub Environment `dev` no tiene "required reviewers" configurados -- nada exige aprobación humana entre disparar `workflow_dispatch` y que `terraform apply -auto-approve` corra contra Azure real.
  evidence: Hallazgo de code review (blind-hunter). Agregar required reviewers es un cambio de configuración del repositorio (Settings → Environments → dev) que requiere confirmación humana explícita, igual que branch protection -- no se activó sin preguntar. Revisar si el ritmo de deploys a dev amerita ese gate adicional.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: `azurerm_container_app.api` no define ningún `probe` (liveness/readiness).
  evidence: Hallazgo de code review (blind-hunter/edge-case-hunter). No existe todavía un endpoint `/health` real contra el cual definir un probe con sentido (el scaffold no mapea rutas fuera de `IsDevelopment()`) -- agregar uno ahora sería apuntar a `/` sin semántica clara. Revisar en la historia 1.5, que agrega el health-check real.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: `azurerm_static_web_app.main` no tiene un backend enlazado (`/api/*`) hacia `azurerm_container_app.api` -- el deploy deja dos apps independientes (Angular y la API) en vez de una integración enrutada.
  evidence: Hallazgo de code review (blind-hunter). Decidir entre "linked backend" de Azure Static Web Apps vs. llamadas CORS directas desde Angular a la API es una decisión de arquitectura que afecta configuración de CORS y URLs base en Angular -- corresponde a la historia 1.5, que hace la primera llamada real de Angular a la API (health-check end-to-end).

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: `cd-dev.yml` no usa cache de capas de Docker (`cache-from`/`cache-to` en `docker/build-push-action`) -- cada disparo manual hace un build completo desde cero pese a que el propio Dockerfile está diseñado con una capa de restore cacheable.
  evidence: Hallazgo de code review (blind-hunter). Solo afecta tiempo de pipeline, no corrección -- revisar si el tiempo de build se vuelve una fricción real para disparos frecuentes.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: Un `terraform apply` que falla a mitad de la creación de `azurerm_container_app.api` deja el recurso registrado en Azure con `provisioningState: Failed`, pero Terraform nunca lo guarda en su state (la API nunca confirmó éxito) -- el siguiente `apply` intenta crearlo de nuevo y Azure responde "ya existe, debe importarse", bloqueando el deploy hasta borrar el objeto manualmente (`az containerapp delete`).
  evidence: Ocurrió 3 veces durante los intentos reales de esta historia (runs 33230451479, 33232144371), cada vez que el Container App falló por un motivo distinto (imagen privada). No hay ningún paso automático que detecte o limpie este estado -- solo se resolvió manualmente vía CLI. Relevante para la historia 1.6 ("destruir el ambiente de dev bajo demanda"): ese workflow debería considerar `terraform apply` fallidos que dejan recursos huérfanos sin reflejarse en el state, no solo el caso feliz de `terraform destroy` sobre un state limpio.

## Deferred from: code review of story-1-4 (2026-08-29)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: Sin scan de vulnerabilidades de imagen (Trivy/Grype) entre el build de la imagen de la API y el push a GHCR.
  evidence: Hallazgo de code review (blind-hunter). Mismo bucket que el pinning de Actions/base images ya diferido en la historia 1.3 -- tratar como política transversal de supply-chain, no como parche aislado de esta historia.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: `src/Api/Dockerfile` no fija un `USER` no-root explícito -- depende implícitamente del default de la imagen base `mcr.microsoft.com/dotnet/aspnet:10.0`.
  evidence: Hallazgo de code review (blind-hunter). Hardening de contenedor razonable pero sin urgencia mientras el scaffold no maneje datos reales (sin lógica de negocio todavía, mismo `Never` congelado del spec de esta historia).

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: `dotnet restore` en el Dockerfile corre sin `--locked-mode` ni `packages.lock.json` -- dos builds del mismo commit podrían resolver versiones de paquete NuGet distintas con el tiempo, lo que debilita la garantía de "tag `:sha` inmutable".
  evidence: Hallazgo de code review (blind-hunter). Cambio de configuración de build/dependencias que corresponde a una historia de CI o de gestión de dependencias, no a esta de CD.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: Sin política de retención/limpieza de tags `:sha` en GHCR -- cada disparo manual publica un tag inmutable nuevo sin expiración, el almacenamiento del paquete crece sin límite.
  evidence: Hallazgo de code review (blind-hunter). Costo operativo a monitorear, no bloqueante mientras el ritmo de deploys a dev sea bajo.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: Sin runbook de rollback documentado para un deploy malo -- el I/O matrix del spec ya acepta "no hay rollback automático, re-disparar el workflow" pero no da los pasos para revertir a un `:sha` anterior conocido-bueno mientras se prepara un fix.
  evidence: Hallazgo de code review (blind-hunter). Documentación operativa, revisar si la frecuencia de deploys a dev lo amerita.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: El nuevo `azurerm_log_analytics_workspace.main` no tiene documentado cómo consultarlo para debug (`az containerapp logs show`, query de ejemplo) -- el recurso existe pero su uso no es descubrible para quien depure un deploy fallido.
  evidence: Hallazgo de code review (blind-hunter). Documentación operativa.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: `terraform plan` no se guarda como artifact del workflow (`actions/upload-artifact`) -- el único registro de qué se estaba por aplicar contra Azure real vive en los logs efímeros del job.
  evidence: Hallazgo de code review (blind-hunter). Trazabilidad/auditoría, no bloqueante para el flujo actual de un solo operador.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: Sin notificación de fallo (Slack/Teams/GitHub issue) cuando el job de deploy falla -- combinado con `cancel-in-progress: false` y el re-disparo manual como único camino de recuperación, un dispatch fallido puede pasar desapercibido hasta que alguien revise la pestaña Actions.
  evidence: Hallazgo de code review (blind-hunter). Observabilidad operativa.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: `web/staticwebapp.config.json` define `navigationFallback` pero no `globalHeaders` (CSP, `X-Content-Type-Options`, `X-Frame-Options`).
  evidence: Hallazgo de code review (blind-hunter). Hardening razonable de agregar cuando el scaffold tenga contenido/lógica real, no bloqueante para un sitio sin datos todavía.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: El `concurrency` group `cd-dev` (`cancel-in-progress: false`) puede descartar silenciosamente un dispatch ya encolado si llega un tercero antes de que termine el primero -- comportamiento estándar de GitHub Actions, solo se conserva el run encolado más reciente.
  evidence: Hallazgo de code review (edge-case-hunter). Bajo riesgo dado que los deploys son manuales e infrecuentes.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-cd-manual-desplegar-el-esqueleto-a-un-ambiente-de-dev.md`
  summary: Nada en `cd-dev.yml` verifica programáticamente que CI esté verde para el SHA desplegado antes de `terraform apply` -- solo `if: github.ref == 'refs/heads/main'`.
  evidence: Decisión del humano (resolución del finding [Review][Decision] de esta pasada): el gate correcto es branch protection en `main` exigiendo los checks `backend`/`frontend` (ya documentado en README, sección "Branch protection"), no un step nuevo en el workflow. Deferred porque branch protection está bloqueado hoy por el plan Free de GitHub con repo privado -- activar en cuanto el plan lo permita.

## Deferred from: code review of story-1-5 (2026-09-03)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-endpoint-de-health-check-verificable-de-punta-a-punta.md`
  summary: `src/Api/Program.cs` sigue llamando `app.UseHttpsRedirection()` sin `app.UseForwardedHeaders()` corriendo detrás del reverse proxy de Azure Container Apps -- el mismo gap flotante desde la historia 1.1, re-diferido en la 1.4 con la nota "revisar en la historia 1.5, que es la primera que necesita una respuesta 200 real".
  evidence: Verificado con evidencia real: el Spec Change Log de la historia 1.4 documenta que el primer smoke-check real contra el FQDN respondió `404` directo (no un `307`), confirmando que `UseHttpsRedirection()` hace no-op (Kestrel no encuentra un puerto HTTPS configurado -- solo `ASPNETCORE_HTTP_PORTS=8080` -- y omite el redirect en vez de loopearlo). No es un bug funcional hoy, pero el código sigue siendo confuso/dead-weight; limpiar (`UseForwardedHeaders` o quitar `UseHttpsRedirection`) cuando se toque `Program.cs` por otra razón.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-endpoint-de-health-check-verificable-de-punta-a-punta.md`
  summary: Los bloques `liveness_probe`/`readiness_probe` de `azurerm_container_app.api` usan los defaults del provider (`initial_delay`, `interval_seconds`, `failure_count_threshold`) sin tuning explícito para el cold-start de `min_replicas = 0`.
  evidence: Hallazgo de code review (blind-hunter). Un app mínimo de .NET 10 arranca en segundos, dentro de la ventana tolerada por los defaults -- bajo riesgo hoy, pero revisar si en el futuro el contenedor gana dependencias de arranque más lentas (conexión a Postgres, etc.).

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-endpoint-de-health-check-verificable-de-punta-a-punta.md`
  summary: El estado `'error'` del shell de Angular no ofrece ningún mecanismo de reintento -- el health-check corre una sola vez en el constructor.
  evidence: Hallazgo de code review (blind-hunter). Mejora de UX razonable, no bloqueante para el AC de esta historia (que solo pide mostrar el estado, éxito o error).

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-endpoint-de-health-check-verificable-de-punta-a-punta.md`
  summary: `GET /health` no fija `Cache-Control: no-store` (ni equivalente) -- una respuesta de estado podría quedar cacheada por un intermediario.
  evidence: Hallazgo de code review (blind-hunter). Bajo riesgo hoy (sin CDN/proxy cacheando explícitamente JSON de API), hardening razonable para cuando el endpoint tenga un consumidor real de monitoreo.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-endpoint-de-health-check-verificable-de-punta-a-punta.md`
  summary: Sin documentación en el README del backend sobre el nuevo endpoint `/health`, la configuración `Cors:AllowedOrigins` necesaria para desarrollo local, ni la convención de que `environment.ts` se sobreescribe en CI.
  evidence: Hallazgo de code review (blind-hunter). Documentación operativa/onboarding, no bloqueante.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-endpoint-de-health-check-verificable-de-punta-a-punta.md`
  summary: Los tests de Angular (`app.spec.ts`) solo simulan un fallo de red (`ProgressEvent('error')`) para el estado `'error'`, no un fallo HTTP real (ej. 500 con body) llegando al mismo callback.
  evidence: Hallazgo de code review (blind-hunter). Mismo código/callback maneja ambos casos en RxJS (`subscribe.error`), así que la cobertura funcional ya existe indirectamente -- separar el escenario es un nice-to-have de test, no una brecha de comportamiento.
