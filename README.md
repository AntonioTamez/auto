# Proyecto Auto

Monorepo con dos codebases independientes: un backend .NET 10 (Clean Architecture) y un workspace Angular.

## Layout

```
src/
  Domain/          # Entidades y reglas de negocio. Sin dependencias hacia afuera.
  Application/      # Casos de uso / interfaces. Depende de Domain.
  Infrastructure/    # Implementaciones (persistencia, servicios externos). Depende de Application + Domain.
  Api/               # ASP.NET Core Web API. Composition root / DI. Depende de Application.
web/                 # Workspace Angular, independiente del backend (sin código compartido).
```

## Comandos

Versiones de toolchain fijadas en `global.json` (.NET SDK `10.0.301`) y `web/.nvmrc` (Node `24.17.0`). `dotnet` resuelve la versión del SDK automáticamente al leer `global.json` desde la raíz del repo; para Node, corre `nvm use` (o instala manualmente la versión de `.nvmrc`) antes de los pasos de `web/`.

### Backend (.NET, desde la raíz del repo)

Orden secuencial para compilar, correr y probar la API:

```
dotnet restore Auto.slnx
dotnet build Auto.slnx
dotnet run --project src/Api
dotnet test Auto.slnx
```

- `dotnet restore` -- descarga los paquetes NuGet. Implícito en `build`/`run`/`test`, pero puedes correrlo aparte para solo descargar dependencias.
- `dotnet build Auto.slnx` -- compila los 4 proyectos (`Domain`, `Application`, `Infrastructure`, `Api`) más `tests/Api.Tests`.
- `dotnet run --project src/Api` -- levanta la API con el perfil `http` de `launchSettings.json` en `http://localhost:5075` (o `https` en `https://localhost:7153` / `http://localhost:5075` con `dotnet run --project src/Api --launch-profile https`). Usa `ASPNETCORE_ENVIRONMENT=Development` por defecto.
- `dotnet test Auto.slnx` -- corre xUnit (`tests/Api.Tests`). Compila en `Debug` por defecto; CI usa `--configuration Release` -- agrega esa flag si quieres reproducir exactamente lo que corre el pipeline.

### Web (Angular, desde `web/`)

Orden secuencial para instalar dependencias, compilar, correr y probar:

```
cd web
npm ci
npm run build
npm start
npm test
```

- `npm ci` -- instala dependencias exactas desde `package-lock.json` (usa `npm install` solo si vas a modificar dependencias).
- `npm run build` -- compila el workspace Angular a `web/dist/` (`ng build`).
- `npm start` -- levanta el dev server (`ng serve`) con recarga en caliente, normalmente en `http://localhost:4200`.
- `npm test` -- corre `ng test` sobre vitest/jsdom.
- `npm run watch` -- variante de build en modo watch (`ng build --watch --configuration development`), útil si no necesitas el dev server completo.

## CI

`.github/workflows/ci.yml` corre en cada `push` (cualquier rama) y en cada `pull_request`, con dos jobs independientes y paralelos:

- **`backend`** -- `actions/setup-dotnet` (lee la versión desde `global.json`) → `dotnet restore` → `dotnet build --no-restore` → `dotnet test --no-build` contra `Auto.slnx`.
- **`frontend`** -- `actions/setup-node` (lee la versión desde `web/.nvmrc`) → `npm ci` → `npm run build` → `npm test` (desde `web/`).

Ambos jobs deben pasar para que un PR sea "verde"; ninguno depende del otro y ambos aparecen como status checks separados en el PR.

### Branch protection (bloquear merge si el pipeline falla)

AD-17 exige que un PR no sea fusionable si el pipeline falla. Esto se resuelve activando **branch protection** en `main` desde GitHub (Settings → Branches → Branch protection rules) requiriendo los status checks `backend` y `frontend`.

**Bloqueado por el plan de GitHub, no por configuración:** con el repo en privado y plan Free, GitHub rechaza esta función (`403 "Upgrade to GitHub Pro or make this repository public to enable this feature"`) aunque el workflow ya corrió en verde (`backend`/`frontend` disponibles como checks). Hasta que se resuelva (GitHub Pro o repo público), el pipeline reporta en cada push/PR pero **no bloquea** el merge — ver `_bmad-output/implementation-artifacts/deferred-work.md`.

Pasos para activarlo en cuanto el plan lo permita:

1. Settings → Branches → Add rule → branch name pattern `main`.
2. Marcar "Require status checks to pass before merging" y seleccionar `backend` y `frontend`.
3. (Recomendado) Marcar "Require branches to be up to date before merging".

Alternativa vía `gh api` (equivalente a los pasos de arriba):

```
gh api repos/AntonioTamez/auto/branches/main/protection \
  --method PUT \
  -H "Accept: application/vnd.github+json" \
  -f 'required_status_checks[strict]=true' \
  -f 'required_status_checks[contexts][]=backend' \
  -f 'required_status_checks[contexts][]=frontend' \
  -F 'enforce_admins=false' \
  -F 'required_pull_request_reviews=null' \
  -F 'restrictions=null'
```

## CD (dev)

`.github/workflows/cd-dev.yml` despliega el esqueleto completo (API + Angular
+ infraestructura Terraform) al ambiente `dev` en Azure. Es **manual
únicamente** (`workflow_dispatch`, AD-17/AD-18) -- nunca corre por push ni
PR, y nunca toca `staging`/`prod` (ese workflow no existe todavía).

### Prerequisitos (ya resueltos, no se repiten en el workflow)

Lo siguiente ya se configuró manualmente fuera de este repo y el workflow
solo lo *consume*, nunca lo crea:

- Bootstrap del state remoto de Terraform (`rg-auto-tfstate`,
  `stautotfstate`, contenedor `tfstate` -- ver `infra/terraform/README.md`).
- Autenticación Azure vía OIDC federado: App Registration
  `gh-actions-auto-cd`, federated credential
  `repo:AntonioTamez/auto:environment:dev`, rol `Contributor` en la
  suscripción + `Storage Blob Data Contributor` en `stautotfstate`.
- GitHub Environment `dev` con los secrets `AZURE_CLIENT_ID`,
  `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`.

### Cómo disparar el deploy manual

Desde la UI de Actions: pestaña **Actions** → workflow **CD (dev)** → **Run
workflow** (rama a desplegar, normalmente `main` con CI en verde).

Desde la CLI:

```
gh workflow run cd-dev.yml
```

El workflow construye y publica la imagen de la API en GHCR
(`ghcr.io/antoniotamez/auto-api:<sha>`, pública -- ver Design Notes de
`spec-1-4` sobre por qué no se usa un PAT), corre `terraform apply` real
contra el resource group `rg-auto-dev`, y despliega el build de Angular al
Static Web App resultante.

### Hiccup esperado en el primer disparo (bootstrap de GHCR)

La primera vez que `cd-dev.yml` corre, el paquete `auto-api` no existe
todavía en GHCR -- el push de la imagen lo crea, pero **como paquete
privado** por default. `terraform apply` puede entonces fallar al
aprovisionar el Container App porque no puede hacer pull de una imagen
privada sin credenciales (el Container App se define sin bloque
`registry`, a propósito, porque la imagen debe ser pública).

Si esto pasa:

1. GitHub → tu perfil/org → **Packages** → `auto-api` → **Package
   settings** → cambiar visibilidad a **Public**.
2. Volver a disparar el mismo workflow (`gh workflow run cd-dev.yml` o
   **Re-run jobs** desde la UI). `terraform apply` es idempotente -- no
   hay efectos secundarios por repetirlo.

Este es un evento único: una vez que el paquete es público, disparos
subsecuentes no lo vuelven a pedir.
