---
title: 'Destruir el ambiente de dev bajo demanda'
type: 'feature'
created: '2026-09-04'
status: 'done'
review_loop_iteration: 1
baseline_commit: 'bceaf10f1332971ee0153e2259a10f76770f7e0c'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-auto-2026-08-19/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El ambiente de dev (historia 1.4) se crea vía CD manual pero nunca se destruye -- queda facturando indefinidamente entre sesiones de trabajo, violando el diseño de "dev efímero" (AD-18).

**Approach:** Agregar un nuevo workflow `destroy-dev.yml`, disparado solo por `workflow_dispatch` sin inputs, que borra el resource group de dev completo vía `az group delete` (API de Azure) -- no `terraform destroy`, porque `azurerm_postgresql_flexible_server.main` y `azurerm_storage_account.main` tienen `prevent_destroy = true` (protección existente de las historias 1.2/1.4 contra un destroy+recreate accidental por drift de config, y ese booleano no puede condicionarse a `var.environment`) -- y luego limpia el blob `dev.tfstate` para que el próximo deploy parta de state limpio. El resource group (`rg-auto-dev`) y el blob de state (`dev.tfstate`) quedan hardcodeados en el YAML, para que jamás pueda alcanzar staging/producción.

## Boundaries & Constraints

**Always:**
- `workflow_dispatch` sin inputs -- `rg-auto-dev` y `dev.tfstate` hardcodeados en el YAML; cambiar el destino exige PR + code review, nunca un input de quien dispara.
- `if: github.ref == 'refs/heads/main'` en el job -- mismo guard que `cd-dev.yml:42`, así solo la versión revisada en `main` del workflow puede ejecutar el borrado real.
- Mismo `concurrency.group: cd-dev` que `cd-dev.yml:12` -- un destroy y un apply sobre `rg-auto-dev`/`dev.tfstate` nunca corren en paralelo.
- Autenticación OIDC (`azure/login`, misma App Registration que `cd-dev.yml`) -- ningún secreto de larga duración; `az` CLI usa esa misma sesión, sin login interactivo adicional.
- `az group delete --name rg-auto-dev --yes` borra el resource group completo a nivel API de Azure -- cascada nativa de Azure sobre todo lo contenido, sin pasar por `prevent_destroy` de Terraform (que solo protege el camino de `terraform apply`/`plan`).
- Tras confirmar el borrado (`az group exists` devuelve `false`), se borra el blob `dev.tfstate` del backend remoto -- el próximo `cd-dev.yml` parte de state vacío y recrea todo desde cero, incluyendo un nuevo sufijo aleatorio -- consistente con "dev es efímero" (AD-18).

**Ask First:** Ninguno -- el enfoque (`az group delete` + limpiar `dev.tfstate`, en vez de `terraform destroy`) ya fue decidido con el humano en esta sesión, específicamente para no tocar `prevent_destroy` en `main.tf`.

**Never:**
- No modifica `prevent_destroy` en `infra/terraform/main.tf` -- esa protección se queda intacta para el camino de `terraform apply` en todos los ambientes.
- No expone ningún input de `workflow_dispatch` para elegir el ambiente o el resource group.
- No borra ni toca `staging.tfstate`/`prod.tfstate` ni ningún resource group que no sea `rg-auto-dev`.
- No corre `terraform destroy`/`apply` contra `dev.tfstate` en este workflow.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Dispatch manual con ambiente de dev desplegado | `rg-auto-dev` existe con recursos reales | `az group delete` borra el resource group completo; `az group exists` confirma `false`; `dev.tfstate` se borra | N/A |
| Dispatch manual sin ambiente desplegado | `rg-auto-dev` no existe | El job detecta que no existe, salta el delete, limpia cualquier `dev.tfstate` residual si existe | El job termina en éxito; log indica "no existe, nada que borrar" |
| `cd-dev.yml` (deploy) corriendo cuando se dispara `destroy-dev.yml` | Mismo `concurrency.group: cd-dev` ocupado | El segundo run se encola, nunca corre en paralelo | N/A -- comportamiento nativo de `concurrency` |

</frozen-after-approval>

## Code Map

- `.github/workflows/destroy-dev.yml` -- (nuevo) workflow completo de esta historia.
- `.github/workflows/cd-dev.yml` -- precedente: `concurrency.group` (línea 12), permisos + `azure/login` OIDC (líneas 15-18, 74-79), branch guard `if: github.ref == 'refs/heads/main'` (línea 42), nombres del backend de state (líneas 25-27: `stautotfstate`/`tfstate`/`dev.tfstate`).
- `infra/terraform/main.tf:144-147,164-166` -- `lifecycle { prevent_destroy = true }` en `azurerm_postgresql_flexible_server.main` y `azurerm_storage_account.main` -- por qué este workflow no usa `terraform destroy`.
- `infra/terraform/locals.tf:10` -- `resource_group_name = "rg-${var.project_prefix}-${var.environment}"` -- confirma que `rg-auto-dev` es el nombre real con los defaults actuales.
- `infra/terraform/variables.tf:34-43` -- `project_prefix` default `"auto"` -- si cambia, el nombre hardcodeado en `destroy-dev.yml` necesita actualizarse a mano.
- `infra/terraform/README.md:70-74` -- backend real (`stautotfstate`/`tfstate`/`dev.tfstate`) -- mismos valores para el blob de state a borrar.

## Tasks & Acceptance

**Execution:**
- [x] `.github/workflows/destroy-dev.yml` -- crear `on: workflow_dispatch` (sin inputs), `if: github.ref == 'refs/heads/main'` en el job, `concurrency: {group: cd-dev, cancel-in-progress: false}`, `permissions: {contents: read, id-token: write}` -- base del job
- [x] `.github/workflows/destroy-dev.yml` -- steps `actions/checkout`, `azure/login` (OIDC) -- autentica tanto el provider como `az` CLI para los pasos siguientes
- [x] `.github/workflows/destroy-dev.yml` -- step que corre `az group exists --name rg-auto-dev`; si es `true`, corre `az group delete --name rg-auto-dev --yes` y reconfirma con `az group exists` que devuelva `false` (falla el job si sigue existiendo); si es `false`, lo reporta y continúa sin error
- [x] `.github/workflows/destroy-dev.yml` -- step que borra el blob `dev.tfstate` (`az storage blob delete --account-name stautotfstate --container-name tfstate --name dev.tfstate --auth-mode login`), tolerante a que el blob no exista
- [x] `.github/workflows/destroy-dev.yml` -- step final `if: always()` que escribe a `$GITHUB_STEP_SUMMARY` si el ambiente fue destruido o si el job falló

**Acceptance Criteria:**
- Given `rg-auto-dev` desplegado con recursos reales, when se dispara `destroy-dev.yml`, then `az group delete` elimina el resource group completo y `az group exists` lo confirma.
- Given `destroy-dev.yml`, when se inspecciona su definición, then no expone ningún input de `workflow_dispatch`, el resource group/blob de state están hardcodeados a `rg-auto-dev`/`dev.tfstate`, y el job solo corre si `github.ref == 'refs/heads/main'`.
- Given `cd-dev.yml` corriendo, when se dispara `destroy-dev.yml` en paralelo, then ambos comparten `concurrency.group: cd-dev` y el segundo se encola.
- Given un borrado exitoso, when termina el job, then el blob `dev.tfstate` ya no existe en el backend remoto.

## Spec Change Log

- **2026-09-04, review_loop_iteration 1** -- Finding (intent_gap, blind-hunter): `terraform destroy` no puede completar porque `azurerm_postgresql_flexible_server.main`/`azurerm_storage_account.main` tienen `prevent_destroy = true` (historias 1.2/1.4), y ese booleano no puede depender de `var.environment` (confirmado vía docs de Terraform: los argumentos de `lifecycle` solo aceptan literales). Amended: el `Approach`/`Boundaries` cambian de `terraform destroy` a `az group delete` + limpieza del blob `dev.tfstate`, decidido con el humano en esta sesión. Avoids: un workflow que nunca puede completar su función principal, y evita tocar `prevent_destroy` (que sigue protegiendo `terraform apply` en todos los ambientes). KEEP: sin inputs de `workflow_dispatch`, mismo `concurrency.group: cd-dev`, autenticación OIDC compartida con `cd-dev.yml` -- todo eso sigue vigente del intento original.
- **2026-09-04, review_loop_iteration 1** -- Finding (bad_spec, edge-case-hunter + verification-gap + blind-hunter, los tres independientemente): el Code Map original escopeó el precedente de `cd-dev.yml` a replicar sin incluir su branch guard (`if: github.ref == 'refs/heads/main'`), dejando que alguien dispare el destroy desde una rama con Terraform o el propio workflow YAML sin revisar. Amended: `Boundaries` y `Tasks` ahora incluyen explícitamente el guard como parte del job base.

## Verification

**Manual checks (if no CLI):**
- Disparar `destroy-dev.yml` manualmente desde la pestaña Actions contra un ambiente de dev real desplegado, confirmar en el log que `az group exists` devuelve `false` tras el delete, y verificar en Azure Portal que `rg-auto-dev` ya no existe.
- Confirmar en el storage account de state (`stautotfstate`/`tfstate`) que `dev.tfstate` fue borrado tras un run exitoso.
- Disparar un `cd-dev.yml` posterior y confirmar que crea el ambiente desde cero sin errores de "ya existe" ni conflictos de state.

## Suggested Review Order

**Por qué no `terraform destroy`**

- Entry point: `prevent_destroy` en Postgres/Storage no puede condicionarse a `var.environment` -- por qué este workflow usa `az group delete` en vez de Terraform.
  [`destroy-dev.yml:49`](../../.github/workflows/destroy-dev.yml#L49)

**Guardas de seguridad (nunca staging/prod)**

- Sin inputs de `workflow_dispatch`: resource group y blob de state hardcodeados en `env`.
  [`destroy-dev.yml:22`](../../.github/workflows/destroy-dev.yml#L22)

- Branch guard -- mismo patrón que `cd-dev.yml:42`, solo la versión revisada en `main` puede borrar de verdad.
  [`destroy-dev.yml:37`](../../.github/workflows/destroy-dev.yml#L37)

- Mismo `concurrency.group` que `cd-dev.yml` -- un destroy y un apply nunca corren en paralelo.
  [`destroy-dev.yml:15`](../../.github/workflows/destroy-dev.yml#L15)

**Borrado del resource group**

- Errores reales de `az group exists` fallan fuerte en vez de leerse silenciosamente como "no existe".
  [`destroy-dev.yml:58`](../../.github/workflows/destroy-dev.yml#L58)

- Inventario de recursos logueado antes del borrado irreversible -- rastro de auditoría de qué se destruyó.
  [`destroy-dev.yml:68`](../../.github/workflows/destroy-dev.yml#L68)

- Reconfirmación post-delete (`az group exists`); falla el job si el resource group sigue existiendo.
  [`destroy-dev.yml:74`](../../.github/workflows/destroy-dev.yml#L74)

**Limpieza de state (`dev.tfstate`)**

- `az storage blob show` distingue `BlobNotFound` (nada que borrar) de un error real (falla el job).
  [`destroy-dev.yml:99`](../../.github/workflows/destroy-dev.yml#L99)

**Auditoría y resultado**

- El resumen registra quién disparó el run y si el blob de state realmente se borró (no lo asume).
  [`destroy-dev.yml:125`](../../.github/workflows/destroy-dev.yml#L125)
