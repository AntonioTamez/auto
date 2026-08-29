# Infraestructura Azure (Terraform)

Módulo Terraform (`azurerm` provider) que define el resource group y los 5
recursos Azure del spine de arquitectura para un ambiente (`dev`, `staging`
o `prod`): Container Apps Environment, Static Web App, PostgreSQL Flexible
Server, Storage Account y Communication Service. Ver `ARCHITECTURE-SPINE.md`
AD-18 y la sección "Deployment & environments".

Todos los nombres y tags se derivan de `locals.tf` a partir de `environment`
y `project_prefix` — ningún recurso hardcodea su propio nombre.

## Bootstrap del state remoto (una sola vez, manual)

El storage account que aloja el `.tfstate` remoto **no** puede vivir dentro
del mismo state que administra (dependencia circular: se necesitaría el
backend para crear el recurso que el backend usa). Por eso se crea una sola
vez, manualmente, vía Azure CLI — fuera de este módulo:

```bash
az group create \
  --name rg-auto-tfstate \
  --location mexicocentral

az storage account create \
  --name stautotfstate \
  --resource-group rg-auto-tfstate \
  --location mexicocentral \
  --sku Standard_LRS \
  --kind StorageV2 \
  --min-tls-version TLS1_2 \
  --allow-blob-public-access false

az storage container create \
  --name tfstate \
  --account-name stautotfstate \
  --auth-mode login

# Habilita blob versioning + soft-delete sobre el storage account de state,
# para poder recuperar un .tfstate sobrescrito o borrado accidentalmente.
az storage account blob-service-properties update \
  --account-name stautotfstate \
  --resource-group rg-auto-tfstate \
  --enable-versioning true \
  --enable-delete-retention true \
  --delete-retention-days 30
```

> **Nota de seguridad:** este storage account aloja el password admin de
> Postgres dentro del `.tfstate` — más sensible que el contenedor de blobs
> que administrará la app. Se bloqueó el acceso público a blobs
> (`--allow-blob-public-access false`), pero **no** se restringió por red
> (`--default-action Deny` + reglas de IP) porque eso requiere decidir de
> antemano qué IPs necesitan acceso (desarrolladores locales, runners de
> CI) — ver `deferred-work.md`.

Ajusta los nombres si ya existen (el storage account debe ser globalmente
único). Este bootstrap se documenta aquí porque es tooling de Terraform, no
uno de los 5 recursos de aplicación que administra este módulo.

## Uso

Desde `infra/terraform/`:

```bash
# Inicializa providers sin requerir el backend real (útil para fmt/validate locales)
terraform init -backend=false

# Inicializa contra el backend remoto real, pasando la config vía -backend-config
# (nunca hardcodeado en providers.tf)
terraform init \
  -backend-config="resource_group_name=rg-auto-tfstate" \
  -backend-config="storage_account_name=stautotfstate" \
  -backend-config="container_name=tfstate" \
  -backend-config="key=dev.tfstate"

terraform fmt -check -recursive
terraform validate

# environment no tiene default a propósito: siempre debe pasarse explícito.
terraform plan -var environment=dev
```

> **Importante:** el `key=...` pasado a `-backend-config` debe coincidir
> siempre con el `-var environment=...` que se está planeando/aplicando —
> usa el patrón `key=${environment}.tfstate` (p. ej. `key=dev.tfstate` para
> `environment=dev`, `key=staging.tfstate` para `environment=staging`). Un
> `key` que no coincide con el `environment` aplicaría el plan de un
> ambiente contra el remote state de otro.

### Autenticación de Azure

`terraform plan`/`validate` en local usan las credenciales ambientales de
Azure CLI (`az login` ya corrido, `az account show` apuntando a la
suscripción correcta) — no se pasan credenciales explícitas en el código.
En CI (historias 1.3/1.4), el pipeline en cambio se autentica con un
service principal / OIDC dedicado, no con `az login` interactivo.

### Verificar `plan` sin backend remoto (antes del bootstrap)

`terraform init -backend=false` (arriba) inicializa providers pero **no**
deja el working directory listo para `terraform plan`: `backend "azurerm" {}`
es un backend real (no local), así que cualquier comando que toque state
—incluido `plan`— falla con `Error: Backend initialization required` hasta
que el backend quede correctamente inicializado. Como el storage account
del bootstrap normalmente aún no existe la primera vez que se quiere
verificar el módulo, usa un override local temporal en vez de inventar
valores de `-backend-config`:

```bash
# 1. Crea un override temporal (ya cubierto por .gitignore: override.tf) que
#    reemplaza el backend real por uno local, solo para esta verificación.
cat > override.tf <<'EOF'
terraform {
  backend "local" {}
}
EOF

# 2. Reinicializa contra ese backend local y corre el plan normalmente.
terraform init -reconfigure
terraform plan -var environment=dev

# 3. Borra el override y vuelve a dejar el directorio apuntando al backend
#    real parcial (sin aplicar nada, sin tocar providers.tf).
rm override.tf
terraform init -reconfigure
```

No borrar `override.tf` antes de terminar deja el módulo apuntando a un
backend local en vez del `azurerm` real declarado en `providers.tf` — no
lo comitees (el `.gitignore` ya lo excluye, pero bórralo del working
directory de todas formas antes de entregar/handoff).

`terraform apply`/`destroy` reales contra Azure **no** se ejecutan a mano
desde esta historia ni desde ningún CLI local de desarrollador (AD-17/AD-18)
— el primer `apply` real ocurre vía pipeline en la historia 1.4. Esta
historia solo verifica que el módulo sea válido y planeable.

## Variables

| Nombre | Default | Descripción |
| --- | --- | --- |
| `environment` | _(ninguno, requerida)_ | `dev`, `staging` o `prod`. Sin default a propósito. |
| `location` | `centralus` | Región de Azure. `mexicocentral` no soporta Container Apps ni Static Web Apps (descubierto en el primer `terraform apply` real, historia 1.4) -- ver comentario en `variables.tf`. |
| `project_prefix` | `auto` | Prefijo de nombre de proyecto en todos los recursos. |
| `api_container_image` | _(ninguno, requerida)_ | Imagen completa (repo:tag) que corre el Container App de la API, ej. `ghcr.io/antoniotamez/auto-api:<sha>`. La pasa `cd-dev.yml`, nunca hardcodeada. |

## Outputs

Ver `outputs.tf` — ids/hostnames de cada recurso, consumidos por la
historia 1.4 (CD) para desplegar la API (.NET) y el build de Angular sin
tener que re-derivarlos. Incluye `container_app_fqdn` (FQDN público de la
API, agregado en la historia 1.4 -- lo consumirá el health-check de la
historia 1.5).
