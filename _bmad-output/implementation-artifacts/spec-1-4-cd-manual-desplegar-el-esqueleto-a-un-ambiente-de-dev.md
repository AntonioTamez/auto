---
title: 'CD manual — desplegar el esqueleto a un ambiente de dev'
type: 'feature'
created: '2026-08-28'
status: 'done'
review_loop_iteration: 0
baseline_commit: '3d6b828a1530f603c906773a8bf6aa6b713cc1ff'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-auto-2026-08-19/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El esqueleto (.NET + Angular) y la infraestructura Terraform existen pero nunca se desplegaron juntos a un Azure real — no hay prueba de que CI verde + Terraform + contenedor + Static Web App funcionen de punta a punta.

**Approach:** Workflow de GitHub Actions (`workflow_dispatch`, `environment: dev`) que construye y publica la imagen de la API en GHCR, corre `terraform apply` real (agregando `azurerm_container_app` + `azurerm_log_analytics_workspace`, que faltaban), y despliega el build de Angular al Static Web App resultante — todo autenticado vía OIDC, sin secretos de larga duración.

## Boundaries & Constraints

**Always:**
- Autenticación Azure vía OIDC federado (ya configurado esta sesión: App Registration `gh-actions-auto-cd`, federated credential `repo:AntonioTamez/auto:environment:dev`, rol `Contributor` en la suscripción + `Storage Blob Data Contributor` en `stautotfstate`, secrets `AZURE_CLIENT_ID`/`AZURE_TENANT_ID`/`AZURE_SUBSCRIPTION_ID` en el GitHub Environment `dev`). El workflow **no** crea estos recursos, solo los consume.
- Imagen de la API: `ghcr.io/antoniotamez/auto-api`, pública (decisión confirmada por el humano — sin PAT de larga duración que gestionar). Tag inmutable `:${{ github.sha }}` es lo que Terraform despliega; `:dev` es solo conveniencia humana.
- `azurerm_container_app_environment.main` requiere `log_analytics_workspace_id` en `azurerm ~> 4.0` (no es opcional) — se agrega `azurerm_log_analytics_workspace` nuevo, cableado a la Environment existente.
- Container App: Consumption plan, `min_replicas = 0` (scale-to-zero, coincide con la decisión de arquitectura), `target_port = 8080` (matching `ASPNETCORE_HTTP_PORTS` en el Dockerfile), sin `registry` block (imagen pública, sin credenciales de pull).
- `terraform apply` corre únicamente desde el workflow (`workflow_dispatch`), nunca desde una CLI local (AD-18).
- El token de deploy del Static Web App (`static_web_app_api_key`, output sensible de Terraform) se enmascara explícitamente (`::add-mask::`) antes de pasarlo entre steps — nunca aparece en logs.

**Ask First:**
- Ninguno pendiente — auth (OIDC), registro (GHCR público) y bootstrap del state ya se resolvieron y ejecutaron en esta sesión antes de escribir este spec.

**Never:**
- No se conecta la API real a Postgres/Storage/Communication Service en esta historia — el contenedor solo expone los endpoints ya existentes del scaffold (sin lógica de negocio nueva). Eso llega con las historias que los necesiten.
- No se usa un PAT de GHCR ni ninguna otra credencial de larga duración — solo `GITHUB_TOKEN` (push de imagen) y OIDC (Azure).
- No se toca `staging`/`prod` — este workflow solo tiene un ambiente (`dev`).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Primer `workflow_dispatch` alguna vez | Paquete `auto-api` no existe en GHCR todavía | El push de imagen lo crea como **privado**; `terraform apply` puede fallar al aprovisionar el Container App porque no puede hacer pull | Documentado como hiccup esperado de bootstrap: el humano marca el paquete público una vez (Settings → Packages), y vuelve a disparar el mismo workflow — `terraform apply` es idempotente |
| `workflow_dispatch` subsecuente, todo verde | Paquete ya público, infra ya existe | `terraform apply` no reporta cambios en recursos existentes salvo el nuevo `azurerm_container_app`/revisión con la imagen nueva; SWA se sobrescribe con el build nuevo | N/A |
| `terraform apply` falla a medio camino | Ej. cuota de Azure excedida | El job falla, no hay rollback automático — el estado remoto refleja lo parcialmente aplicado | Re-disparar el mismo workflow una vez resuelta la causa; `apply` es idempotente |

</frozen-after-approval>

## Code Map

- `src/Api/Dockerfile` -- (nuevo) build multi-stage: `mcr.microsoft.com/dotnet/sdk:10.0` (restore/publish, aprovechando `global.json` 10.0.301) → `mcr.microsoft.com/dotnet/aspnet:10.0` (runtime), `ENV ASPNETCORE_HTTP_PORTS=8080`, `ENTRYPOINT ["dotnet", "Api.dll"]`
- `.dockerignore` -- (nuevo, raíz) excluye `.git/`, `.github/`, `_bmad-output/`, `_bmad/`, `infra/`, `web/node_modules/`, `web/dist/`, `**/bin`, `**/obj`, `tests/`, `docs/` -- contexto de build = raíz del repo (Dockerfile hace `COPY src/...`)
- `infra/terraform/main.tf` -- agregar `azurerm_log_analytics_workspace.main` (sku `PerGB2018`, retención 30 días); agregar `log_analytics_workspace_id` a `azurerm_container_app_environment.main`; agregar `azurerm_container_app.api` (min_replicas 0, target_port 8080, imagen desde nueva variable)
- `infra/terraform/variables.tf` -- agregar `api_container_image` (string, sin default, requerida -- la pasa el workflow con el tag `:sha` real)
- `infra/terraform/outputs.tf` -- agregar output del FQDN del Container App (consumido por la historia 1.5, health-check)
- `web/staticwebapp.config.json` -- (nuevo) `navigationFallback` a `/index.html` para ruteo SPA en Azure Static Web Apps
- `.github/workflows/cd-dev.yml` -- (nuevo) `workflow_dispatch`, `environment: dev`, `permissions: {contents: read, id-token: write, packages: write}`: build+push imagen (docker/setup-buildx-action, docker/build-push-action) → `azure/login@v2` (OIDC) → `terraform init` (backend real `stautotfstate`/`tfstate`/`key=dev.tfstate`) → `terraform apply -auto-approve` → build Angular → deploy a Static Web App (`Azure/static-web-apps-deploy@v1`, token enmascarado desde `terraform output`)
- `README.md` -- documentar cómo disparar el deploy manual, el hiccup de bootstrap del paquete GHCR privado la primera vez, y que la auth OIDC/bootstrap del state ya están configurados (no se repiten)
- Referencia normativa: `epics.md` Story 1.4 AC; `epic-1-context.md` Cross-Story Dependencies (1.4 es prerequisito de 1.5/1.6/1.8); `ARCHITECTURE-SPINE.md` AD-17/AD-18 y sección "CI/CD & environment lifecycle"
- Ya ejecutado esta sesión, fuera del diff de código (no re-derivar en step-03): bootstrap del state remoto (`rg-auto-tfstate`, `stautotfstate`, contenedor `tfstate`, versioning/soft-delete); App Registration + Service Principal + federated credential OIDC; roles `Contributor`/`Storage Blob Data Contributor`; GitHub Environment `dev` con los 3 secrets Azure

## Tasks & Acceptance

**Execution:**
- [x] `src/Api/Dockerfile` -- build multi-stage descrito arriba -- produce la imagen que Container Apps va a correr
- [x] `.dockerignore` -- excluir directorios no relevantes al build -- contexto de build chico y limpio
- [x] `infra/terraform/main.tf` -- `azurerm_log_analytics_workspace` + wiring a la Environment + `azurerm_container_app.api` -- corrige el gap de la historia 1.2 (Environment sin workspace no es válida en azurerm ~>4.0) y agrega el recurso que faltaba para correr la API
- [x] `infra/terraform/variables.tf` -- variable `api_container_image` -- desacopla el tag de imagen del código Terraform, lo inyecta el workflow
- [x] `infra/terraform/outputs.tf` -- output del FQDN del Container App -- lo necesitará la historia 1.5 para el health-check end-to-end
- [x] `web/staticwebapp.config.json` -- navigationFallback -- rutas de Angular (no archivos estáticos reales) no devuelvan 404 en Azure Static Web Apps
- [x] `.github/workflows/cd-dev.yml` -- workflow completo descrito arriba -- cumple el AC: `workflow_dispatch` → `terraform apply` real → esqueleto desplegado
- [x] `README.md` -- documentar trigger manual, prerequisitos ya resueltos, y el hiccup de bootstrap de GHCR -- reproducibilidad

> **Nota de verificación (2026-08-28):** todo el código de esta historia está escrito y verificado estáticamente (`terraform fmt`/`validate`/`plan` reales contra el provider v4.81.0, `dotnet restore`/`publish` reproduciendo exactamente los comandos del Dockerfile, YAML del workflow parseado). Lo que **no** se ejecutó desde este entorno, a propósito: `docker build` real (sin daemon Docker disponible en el sandbox) y el disparo real de `cd-dev.yml` vía `workflow_dispatch` (requiere acción humana explícita -- AD-18 prohíbe que cualquier `terraform apply` corra fuera del workflow, y este agente no es el pipeline). Las Acceptance Criteria que dependen de un `apply` real contra Azure (FQDN respondiendo, SWA sirviendo el fallback SPA) quedan pendientes de ese primer disparo humano.

**Acceptance Criteria:**
- Given el pipeline de CI en verde, when se dispara manualmente `cd-dev.yml` vía `workflow_dispatch`, then se ejecuta `terraform apply` real contra Azure y se aprovisiona (o actualiza) el resource group de dev con los 7 recursos (5 del spine original + Log Analytics Workspace + Container App).
- Given el `terraform apply` exitoso, when se visita el FQDN del Container App, then la API responde (aunque sea con los endpoints por defecto del scaffold, sin lógica de negocio nueva).
- Given el build de Angular, when se despliega al Static Web App, then el sitio carga y las rutas de Angular no devuelven 404 (fallback a `index.html`).
- Given cualquier ejecución del workflow, when se revisan los logs, then el token de deploy del Static Web App nunca aparece en texto plano.
- Given el código del workflow y de Terraform, when se revisan, then ningún `terraform apply`/`destroy` se ejecuta desde una CLI local -- solo desde `workflow_dispatch` (AD-18).

### Review Findings

- [x] [Review][Patch] `cd-dev.yml` corría `terraform apply -auto-approve` sin `fmt -check`/`validate`/`plan` visible antes [`.github/workflows/cd-dev.yml:98`] — el primer apply real contra Azure no tenía ningún gate estático. **Aplicado:** agregados `terraform fmt -check`, `terraform validate`, y `terraform plan -out=tfplan` (aplicado desde el archivo, no un plan implícito).
- [x] [Review][Patch] `.dockerignore` solo excluía `web/node_modules/`/`web/dist/`, dejando entrar todo el código fuente de Angular al contexto de build de la API [`.dockerignore:10`]. **Aplicado:** excluye `web/` completo.
- [x] [Review][Patch] Sin verificación post-deploy de que el esqueleto quedara realmente alcanzable, y sin acoplamiento verificado entre `target_port` (Terraform) y `ASPNETCORE_HTTP_PORTS` (Dockerfile) — un drift entre ambos pasaría silenciosamente en verde [`infra/terraform/main.tf:77`, `src/Api/Dockerfile:47`]. **Aplicado:** step de smoke-check al final del workflow (curl tolerante a cold-start/cualquier respuesta HTTP, solo falla en fallo real de conexión) + resumen en `$GITHUB_STEP_SUMMARY`.
- [x] [Review][Patch] `api_container_image` sin validación -- un valor vacío o sin tag pasaría directo a `terraform apply` [`infra/terraform/variables.tf:38`]. **Aplicado:** `validation` block exigiendo formato `repo:tag`.
- [x] [Review][Patch] Token de deploy del Static Web App capturado con `echo "token=$token" >> $GITHUB_OUTPUT` -- rompe si el valor contiene un salto de línea [`.github/workflows/cd-dev.yml:106` (antes del patch)]. **Aplicado:** formato multilínea-seguro con delimitador heredoc.
- [x] [Review][Patch] `workflow_dispatch` sin restricción de rama -- se podía disparar un deploy real a `dev` desde cualquier rama, no solo `main` con CI en verde [`.github/workflows/cd-dev.yml:39`]. **Aplicado:** `if: github.ref == 'refs/heads/main'` a nivel de job.
- [x] [Review][Patch] Sin `ASPNETCORE_ENVIRONMENT` en el Container App -- el contenedor corre sin ambiente explícito [`infra/terraform/main.tf:66`]. **Aplicado:** `env { ASPNETCORE_ENVIRONMENT = "Production" }`.
- [x] [Review][Patch] `web/staticwebapp.config.json` -- lista de extensiones del `navigationFallback.exclude` incompleta (faltaban fuentes, webmanifest, source maps, etc.) [`web/staticwebapp.config.json:4`]. **Aplicado:** lista ampliada.
- [x] [Review][Patch] `infra/terraform/README.md` -- tablas de Variables/Outputs no reflejaban `api_container_image`/`container_app_fqdn` [`infra/terraform/README.md:144`]. **Aplicado:** tablas actualizadas.
- [x] [Review][Patch] Tag flotante `:dev` publicado en GHCR sin documentar su propósito [`.github/workflows/cd-dev.yml:69`]. **Aplicado:** comentario aclarando que es solo conveniencia humana, Terraform siempre usa `:sha`.
- [x] [Review][Fuera del diff] Verificado (`az provider show`) que el resource provider `Microsoft.Communication` -- requerido por `azurerm_communication_service`, uno de los 5 recursos del spine -- **no estaba registrado** en la suscripción; el primer `terraform apply` real habría fallado en ese recurso. **Aplicado:** `az provider register --namespace Microsoft.Communication`, confirmado `Registered` antes de disparar el deploy.
- [x] [Review][Defer] Sin wiring de identidad/RBAC ni variables de conexión a Postgres/Storage/Communication Service en el Container App -- deliberado, dentro del `Never` congelado de esta historia; los gaps pre-existentes de la historia 1.2 ("revisar en la historia 1.4") siguen abiertos y se re-registran en `deferred-work.md` apuntando a la próxima historia que conecte la API a esos recursos.
- [x] [Review][Defer] Sin required reviewers en el GitHub Environment `dev` como gate humano antes de un `apply` real -- deferred, decisión de settings del repo que requiere confirmación humana explícita.
- [x] [Review][Defer] Sin probe (liveness/readiness) en el Container App -- deferred a la historia 1.5, que agrega el primer endpoint `/health` real contra el cual definir el probe.
- [x] [Review][Defer] Static Web App sin backend enlazado (`/api/*`) al Container App -- deferred a la historia 1.5, que hace la primera llamada real de Angular a la API.
- [x] [Review][Defer] Sin cache de capas Docker (`cache-from`/`cache-to`) en `cd-dev.yml` -- deferred, optimización de velocidad de build, no de corrección.
- [x] [Review][Defer] Tag flotante `sdk:10.0` en el Dockerfile sin pinnear al feature band exacto `10.0.301` de `global.json` -- deferred, riesgo de baja probabilidad; pinnear ahora crea carga de mantenimiento propia (YAGNI).

## Spec Change Log

- 2026-08-29 -- Primer disparo real de `cd-dev.yml` (run 33229190090) falló en `azure/login`: el subject claim que GitHub presenta realmente es `repo:{owner}@{owner_id}/{repo}@{repo_id}:environment:dev` (con IDs numéricos), no `repo:{owner}/{repo}:environment:dev` como se configuró originalmente. **Aplicado:** federated credential actualizada al subject real (`repo:AntonioTamez@4588583/auto@1339795123:environment:dev`) vía `az ad app federated-credential update`.
- 2026-08-29 -- Segundo disparo (run 33229283273) pasó el login pero `terraform apply` falló: **`mexicocentral` no soporta `Microsoft.App/managedEnvironments` (Container Apps) ni `Microsoft.Web/staticSites` (Static Web Apps)** -- 2 de los 5 recursos del spine, error 400 `LocationNotAvailableForResourceType` de la API de Azure. La historia 1.2 nunca lo detectó porque solo corrió `terraform plan`, nunca un `apply` real contra esos tipos de recurso. Alcanzó a crear 4 recursos reales (resource group, Postgres, Storage, Log Analytics, Communication Service) antes de fallar. **Aplicado (decisión del humano):** `az group delete rg-auto-dev` para limpiar el ambiente parcial (sin datos reales, costo mínimo); `location` default cambiado de `mexicocentral` a `centralus` en `variables.tf` (soporta los 7 tipos de recurso); tabla de `infra/terraform/README.md` actualizada. El próximo `terraform apply` recreará todo desde cero en `centralus` -- el state remoto se auto-corrige vía refresh (no se tocó `dev.tfstate` manualmente, no tengo permisos de blob-data con mi cuenta personal, solo el service principal de CI los tiene).
- 2026-08-29 -- Tercer disparo (run 33230001937) recreó todo en `centralus` correctamente hasta `azurerm_container_app.api`, que falló porque el paquete `auto-api` en GHCR seguía privado (el hiccup ya documentado en README/spec) -- confirmado que `gh` no tenía scope `packages` para arreglarlo por API; el humano lo marcó público manualmente.
- 2026-08-29 -- Cuarto disparo (run 33230451479) reveló dos problemas nuevos: (1) el Container App que falló en el run anterior quedó registrado en Azure con `provisioningState: Failed` aunque Terraform nunca lo guardó en su state (el apply falló antes de que la API confirmara éxito) -- el siguiente `apply` intentó crearlo de nuevo y Azure respondió "ya existe, debe importarse". (2) `azurerm_postgresql_flexible_server.main` -- exitosamente creado en el run anterior -- disparó un intento de "Modifying" sobre `zone` (Azure asigna la zona de disponibilidad automáticamente al crear, no se fija en el código) y Azure lo rechazó (`zone` solo puede cambiarse intercambiando con `high_availability.standby_availability_zone`). **Aplicado:** `az containerapp delete ca-auto-dev` (objeto roto, sin nada que preservar) para que el próximo `apply` lo cree limpio; `lifecycle { ignore_changes = [zone] }` agregado a `azurerm_postgresql_flexible_server.main` para que Terraform deje de intentar gestionar ese valor computado por Azure.
- 2026-08-29 -- Quinto disparo (run 33230677818): mismo error de imagen privada -- la visibilidad "Public" no había quedado aplicada (confirmado con `curl` directo contra `ghcr.io/token`, independiente de Azure). Causa real: el paquete tenía activado "Inherit access from source repository", que ignora el toggle manual de visibilidad mientras el repo sea privado. El humano lo desmarcó y volvió a aplicar "Change visibility → Public"; confirmado con `curl` (token real emitido, manifest devuelve 200 con el `Accept` header correcto). Dejó otro Container App roto (mismo patrón del punto anterior) -- borrado de nuevo antes de reintentar.
- 2026-08-29 -- Sexto disparo (run 33232144371): falló otra vez en "already exists" -- el Container App roto del quinto intento (que también falló, antes de la corrección de "Inherit access") no se había limpiado todavía. Borrado y reintentado.
- 2026-08-29 -- **Séptimo disparo (run 33232247122): exitoso.** Los 7 recursos del spine (+ Log Analytics) provisionados en `centralus`; imagen publicada y jalada correctamente desde GHCR; Angular desplegado al Static Web App. Verificado manualmente: API responde `404` (esperado, sin endpoints reales aún -- historia 1.5 lo resuelve), Static Web App responde `200` en `/` y en una ruta SPA no real (`/comparar`, fallback funcionando). URLs: API `https://ca-auto-dev.agreeablecoast-87d50e5b.centralus.azurecontainerapps.io`, Web `https://icy-tree-03456ff10.7.azurestaticapps.net`. Limpiado además un input inválido (`config_file_location`, no reconocido por `Azure/static-web-apps-deploy@v1`) que generaba un warning inofensivo en el log.

## Design Notes

**Por qué `min_replicas = 0`:** coincide con la decisión de arquitectura ("Container Apps consumption/scale-to-zero") y minimiza costo en un ambiente de dev que no necesita estar siempre caliente -- el primer request después de estar en cero paga un cold-start, aceptable para este ambiente.

**Por qué GHCR público en vez de un PAT:** un PAT de larga duración es un secreto que hay que rotar y proteger; hoy la imagen no contiene nada sensible (sin lógica de negocio, sin datos), así que el costo de gestionar ese secreto no se justifica todavía. Si el contenido de la imagen deja de ser trivial, revisar esta decisión (ver `deferred-work.md`).

**Por qué el hiccup de bootstrap es aceptable:** es un evento único (la primera vez que el paquete GHCR se crea) y `terraform apply` es idempotente -- volver a disparar el mismo workflow después de marcar el paquete público completa el despliegue sin efectos secundarios.

## Verification

**Commands:**
- `docker build -f src/Api/Dockerfile .` (desde la raíz del repo) -- expected: build exitoso, produce una imagen corrible
- `terraform validate` / `terraform fmt -check -recursive` (desde `infra/terraform/`) -- expected: sin errores
- `terraform plan -var environment=dev -var api_container_image=ghcr.io/antoniotamez/auto-api:test` -- expected: plan muestra el Log Analytics Workspace + Container App a crear, sin errores del provider
- Disparo real de `cd-dev.yml` vía `gh workflow run` o UI de Actions -- expected: job completo en verde, FQDN del Container App responde, Static Web App carga

**Manual checks (if no CLI):**
- Revisar el Container App en Azure Portal: estado "Running", al menos una réplica cuando hay tráfico.
- Revisar que el Static Web App sirva `index.html` al navegar a una ruta de Angular que no es un archivo real (ej. `/comparar`).

## Suggested Review Order

**Pipeline de CD (punto de entrada)**

- Entry point: `workflow_dispatch` únicamente, restringido a `main` -- ningún deploy real desde una rama sin probar.
  [`cd-dev.yml:42`](../../.github/workflows/cd-dev.yml#L42)

- Build+push de la imagen de la API a GHCR (pública, sin PAT) antes de tocar Azure.
  [`cd-dev.yml:60`](../../.github/workflows/cd-dev.yml#L60)

- `terraform apply` corre desde un plan guardado (`fmt`/`validate`/`plan -out` primero) -- el primer apply real contra Azure queda visible en el log antes de ejecutarse.
  [`cd-dev.yml:117`](../../.github/workflows/cd-dev.yml#L117)

- Smoke check final: confirma que el Container App es alcanzable (tolerante a cold-start) y escribe el resumen del deploy.
  [`cd-dev.yml:175`](../../.github/workflows/cd-dev.yml#L175)

**Infraestructura Terraform (recursos nuevos)**

- `azurerm_log_analytics_workspace` -- cierra el gap de la historia 1.2 (la Environment no es válida sin él en azurerm ~>4.0).
  [`main.tf:34`](../../infra/terraform/main.tf#L34)

- `azurerm_container_app_environment` ahora cableada al workspace.
  [`main.tf:43`](../../infra/terraform/main.tf#L43)

- `azurerm_container_app.api`: Consumption, `min_replicas = 0`, sin bloque `registry` (imagen pública).
  [`main.tf:55`](../../infra/terraform/main.tf#L55)

- `ingress.target_port = 8080` -- debe coincidir exactamente con `ASPNETCORE_HTTP_PORTS` del Dockerfile (verificado en runtime por el smoke check de arriba, no solo por comentario).
  [`main.tf:80`](../../infra/terraform/main.tf#L80)

- `api_container_image`: sin default, validada con formato `repo:tag` -- el workflow decide el tag real, nunca Terraform.
  [`variables.tf:38`](../../infra/terraform/variables.tf#L38)

- `container_app_fqdn`: nuevo output que la historia 1.5 consumirá para el health-check end-to-end.
  [`outputs.tf:16`](../../infra/terraform/outputs.tf#L16)

**Imagen de contenedor de la API**

- Build multi-stage: SDK completo para restore/publish, runtime `aspnet` sin SDK.
  [`Dockerfile:13`](../../src/Api/Dockerfile#L13)

- `ENTRYPOINT` fijo al ensamblado publicado.
  [`Dockerfile:52`](../../src/Api/Dockerfile#L52)

**Static Web App / Angular**

- `navigationFallback` a `index.html` para que rutas de Angular (no archivos reales) no devuelvan 404.
  [`staticwebapp.config.json:2`](../../web/staticwebapp.config.json#L2)

**Periféricos**

- `.dockerignore` excluye `web/` completo del contexto de build de la API (fix de review, antes solo excluía `node_modules`/`dist`).
  [`.dockerignore:10`](../../.dockerignore#L10)

- Documentación del trigger manual, prerequisitos ya resueltos (OIDC, bootstrap del state), y el hiccup esperado de bootstrap de GHCR.
  [`README.md:91`](../../README.md#L91)
