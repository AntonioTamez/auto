---
title: 'Infraestructura Azure definida en Terraform'
type: 'feature'
created: '2026-08-20'
status: 'done'
review_loop_iteration: 0
baseline_commit: '7a8ce444702b9d654f6d592aee1e502c6cde7899'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-auto-2026-08-19/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No existe infraestructura de Azure definida en código. Historias futuras (CI/CD, health-check, monitoreo) necesitan un ambiente real creable/destruible sin ClickOps, y AD-18 exige que todo recurso Azure sea Terraform-managed desde el inicio.

**Approach:** Módulo Terraform (`azurerm` provider) en `infra/terraform/` que define resource group + los 5 recursos del spine (Container Apps Environment, Static Web App, PostgreSQL Flexible Server, Storage Account, Communication Service), parametrizado por `environment` para reutilizarse en dev/staging/prod. State remoto en Azure Storage vía backend parcial. Región: `mexicocentral`.

## Boundaries & Constraints

**Always:**
- Los 6 recursos (resource group + 5) viven en un único `azurerm_resource_group`; ningún recurso referencia un `resource_group_name` externo (evita huérfanos al destruir, AC del épico).
- Nombres y tags de todo recurso se derivan de `locals.tf` (única fuente), incluyendo siempre `environment` (dev/staging/prod) — nunca hardcodeados por recurso.
- Región fija `mexicocentral` (decidido: costo equivalente a South Central US, mejor latencia/residencia para Monterrey).
- Backend de state: `backend "azurerm" {}` parcial (sin valores hardcodeados) — el storage account/container reales se pasan vía `-backend-config` en `init`, nunca en el código versionado.
- PostgreSQL Flexible Server usa tier Burstable B1ms; Storage Account cubre Blob Storage; Communication Service solo email (spine AD-4).

**Ask First:**
- Si Container Apps requiere solo `azurerm_container_app_environment` (plataforma) o también un `azurerm_container_app` placeholder con imagen pública temporal — aún no existe imagen real de la app (llega en historia 1.4).
- Prefijo de nombre de proyecto en recursos (propuesto `auto`, dado que el nombre de producto sigue siendo placeholder en el PRD) — confirmar antes de fijarlo en `locals.tf`.

**Never:**
- No gestionar el storage account del backend remoto dentro del mismo state que administra (dependencia circular) — su creación se documenta como bootstrap manual vía Azure CLI en el README, fuera de este state.
- No ejecutar `terraform apply`/`destroy` reales contra Azure en esta historia (verificación es `validate`/`plan` únicamente; el primer `apply` real ocurre en la historia 1.4 vía pipeline, honrando AD-17/AD-18).
- No provisionar recursos de staging/producción en esta historia — la variable `environment` queda genérica pero solo se ejercita con `dev`.

</frozen-after-approval>

## Code Map

- `infra/terraform/providers.tf` -- (nuevo) bloque `terraform{}` con `required_providers.azurerm`, `backend "azurerm" {}` parcial, `provider "azurerm" { features {} }`
- `infra/terraform/variables.tf` -- (nuevo) `environment` (sin default, requerida), `location` (default `mexicocentral`), `project_prefix` (default `auto`)
- `infra/terraform/locals.tf` -- (nuevo) nombres de recursos y mapa de tags comunes, única fuente de convención de naming
- `infra/terraform/main.tf` -- (nuevo) `azurerm_resource_group` + los 5 recursos del spine
- `infra/terraform/outputs.tf` -- (nuevo) ids/hostnames que historias 1.4+ (CD) necesitarán consumir
- `infra/terraform/README.md` -- (nuevo) bootstrap manual del storage account de state (comandos Azure CLI, una sola vez) + comandos de uso (`init`/`validate`/`plan`)
- `.gitignore` -- (editar) agregar bloque Terraform: `.terraform/`, `*.tfstate`, `*.tfstate.backup`, `override.tf*`, `*.auto.tfvars` (mantener `.terraform.lock.hcl` versionado)
- Referencia normativa: `ARCHITECTURE-SPINE.md` AD-18 (líneas 146-150), Stack (162-181), sección "Deployment & environments" (198-231)
- Tooling confirmado en este entorno: Terraform `1.15.8` (instalado vía winget en esta sesión), Azure CLI `2.89.0` (ya autenticado, suscripción Pay-As-You-Go) -- `terraform plan` puede correr con credenciales reales sin `az login` adicional

## Tasks & Acceptance

**Execution:**
- [x] `infra/terraform/providers.tf` -- declarar `terraform{}`/`required_providers`/backend parcial/`provider` -- fija versión del provider y difiere config real del backend a `init`
- [x] `infra/terraform/variables.tf` -- declarar `environment`/`location`/`project_prefix` -- parametriza dev/staging/prod
- [x] `infra/terraform/locals.tf` -- nombres de recursos + tags comunes (incluye `environment`) -- fuente única de naming/tagging
- [x] `infra/terraform/main.tf` -- resource group + `azurerm_container_app_environment`, `azurerm_static_web_app`, `azurerm_postgresql_flexible_server` (Burstable B1ms), `azurerm_storage_account`, `azurerm_communication_service` -- los 5 recursos + RG del AC del épico
- [x] `infra/terraform/outputs.tf` -- exponer ids/hostnames relevantes para CD -- evita que la historia 1.4 tenga que re-derivarlos
- [x] `infra/terraform/README.md` -- documentar bootstrap del storage account de state y comandos `init`/`validate`/`plan` -- reproducibilidad
- [x] `.gitignore` -- agregar bloque Terraform -- evita comitear state/artefactos locales
- [x] Ejecutar `terraform fmt -check`, `terraform validate`, `terraform plan -var environment=dev` -- confirma que el módulo es sintácticamente válido y planea los 6 recursos sin error

**Acceptance Criteria:**
- Given el código Terraform en `infra/terraform/`, when se ejecuta `terraform validate`, then no reporta errores de sintaxis ni de referencias.
- Given el mismo código, when se ejecuta `terraform plan -var environment=dev`, then el plan muestra la creación de un resource group + los 5 recursos del spine, todos con tags que incluyen `environment=dev`, sin errores del provider `azurerm`.
- Given `main.tf`, when se revisa cada recurso, then ninguno referencia un `resource_group_name` distinto al resource group definido en el mismo archivo (garantiza que un futuro `terraform destroy` no deja huérfanos).

## Spec Change Log

## Design Notes

El storage account que aloja el state remoto no puede vivir en el mismo state que administra (problema circular: se necesitaría el backend para crear el recurso que el backend usa). Se resuelve documentando su creación como bootstrap manual de una sola vez vía Azure CLI en `README.md` — no es parte de los 5 recursos del AC (que son recursos de la aplicación, no de tooling de Terraform).

`environment` no tiene default a propósito: obliga a pasar `-var environment=dev|staging|prod` explícitamente en cada `plan`/`apply`, evitando que un `apply` sin flags aterrice silenciosamente en el ambiente equivocado.

## Verification

**Commands:**
- `terraform fmt -check -recursive` (desde `infra/terraform/`) -- expected: sin diffs
- `terraform init -backend=false` (desde `infra/terraform/`) -- expected: inicializa providers sin requerir backend real
- `terraform validate` (desde `infra/terraform/`) -- expected: `Success! The configuration is valid.`
- `terraform plan -var environment=dev` (desde `infra/terraform/`, usando credenciales de Azure CLI ya autenticadas; requiere el override de backend local documentado en `infra/terraform/README.md` § "Verificar plan sin backend remoto" mientras el storage account de state no exista) -- expected: 8 to add (resource group + 5 recursos del spine + 2 recursos `random_*` auxiliares), sin errores del provider

**Manual checks (if no CLI):**
- Revisar `locals.tf`: es la única fuente de nombres/tags; ningún recurso en `main.tf` hardcodea su propio nombre o tags.
- Revisar `providers.tf`: el bloque `backend "azurerm" {}` no contiene valores reales (storage account, container, key) hardcodeados.

## Suggested Review Order

**Recursos y contención en el resource group**

- Entry point: todos los recursos cuelgan de este resource group — ningún `resource_group_name` externo, evita huérfanos al destruir.
  [`main.tf:25`](../../infra/terraform/main.tf#L25)

- Los 5 recursos del spine, cada uno referenciando `azurerm_resource_group.main` para nombre/ubicación.
  [`main.tf:31`](../../infra/terraform/main.tf#L31)

**Endurecimiento y correctitud (findings de code review)**

- Storage Account: TLS mínimo forzado y acceso público a blobs anidados deshabilitado.
  [`main.tf:66`](../../infra/terraform/main.tf#L66)

- Password de Postgres generada con mínimos garantizados por categoría de carácter, para cumplir la política de complejidad de Azure.
  [`main.tf:19`](../../infra/terraform/main.tf#L19)

**Naming y tags (fuente única)**

- `postgresql_server_name` envuelto en `lower()` por consistencia con `storage_account_name`.
  [`locals.tf:19`](../../infra/terraform/locals.tf#L19)

- Todo tag/nombre deriva de aquí; ningún recurso lo hardcodea.
  [`locals.tf:4`](../../infra/terraform/locals.tf#L4)

**Validación de inputs**

- `project_prefix` validado por regex antes de interpolarse en nombres con restricciones de Azure (evita error confuso del provider).
  [`variables.tf:27`](../../infra/terraform/variables.tf#L27)

- `environment` sin default a propósito, para forzar un flag explícito en cada `plan`/`apply`.
  [`variables.tf:1`](../../infra/terraform/variables.tf#L1)

**Backend y outputs para la historia 1.4**

- Backend parcial: valores reales del state remoto nunca se hardcodean aquí.
  [`providers.tf:15`](../../infra/terraform/providers.tf#L15)

- Ids/hostnames expuestos para que la historia 1.4 (CD) no tenga que re-derivarlos.
  [`outputs.tf:1`](../../infra/terraform/outputs.tf#L1)

**Documentación (bootstrap y verificación reproducible)**

- Recipe documentada para correr `plan` sin backend remoto — cierra el gap de verificación hallado en review.
  [`README.md:89`](../../infra/terraform/README.md#L89)

- Advertencia: el `key` del backend debe coincidir siempre con `-var environment`.
  [`README.md:74`](../../infra/terraform/README.md#L74)

- Bootstrap manual del storage account de state, con versioning/soft-delete habilitados.
  [`README.md:12`](../../infra/terraform/README.md#L12)

**Periféricos**

- Bloque Terraform agregado al `.gitignore` raíz.
  [`.gitignore:24`](../../.gitignore#L24)
