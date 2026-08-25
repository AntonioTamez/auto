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

## Build

Backend (desde la raíz del repo, vía la solución `Auto.slnx`):

```
dotnet build Auto.slnx
```

Frontend (desde `web/`):

```
npm run build
```

## Ejecutar la API

```
dotnet run --project src/Api
```

Requiere haber corrido `dotnet restore` (implícito en `dotnet build`/`dotnet run`) al menos una vez. Versiones de toolchain fijadas en `global.json` (.NET SDK) y `web/.nvmrc` (Node).

## Tests

Backend (xUnit, proyecto `tests/Api.Tests`, incluido en `Auto.slnx`):

```
dotnet test Auto.slnx
```

Frontend (Angular, `ng test` corre sobre vitest/jsdom):

```
cd web
npm ci
npm test
```

## CI

`.github/workflows/ci.yml` corre en cada `push` (cualquier rama) y en cada `pull_request`, con dos jobs independientes y paralelos:

- **`backend`** -- `actions/setup-dotnet` (lee la versión desde `global.json`) → `dotnet restore` → `dotnet build --no-restore` → `dotnet test --no-build` contra `Auto.slnx`.
- **`frontend`** -- `actions/setup-node` (lee la versión desde `web/.nvmrc`) → `npm ci` → `npm run build` → `npm test` (desde `web/`).

Ambos jobs deben pasar para que un PR sea "verde"; ninguno depende del otro y ambos aparecen como status checks separados en el PR.

### Branch protection (bloquear merge si el pipeline falla)

AD-17 exige que un PR no sea fusionable si el pipeline falla. Esto se resuelve activando **branch protection** en `main` desde GitHub (Settings → Branches → Branch protection rules) requiriendo los status checks `backend` y `frontend`:

1. Que exista al menos un run del workflow (para que GitHub ofrezca `backend`/`frontend` como checks disponibles).
2. Settings → Branches → Add rule → branch name pattern `main`.
3. Marcar "Require status checks to pass before merging" y seleccionar `backend` y `frontend`.
4. (Recomendado) Marcar "Require branches to be up to date before merging".

Este paso mutaría configuración real del repositorio en GitHub, por lo que no se activó automáticamente como parte de esta historia -- requiere confirmación humana explícita antes de ejecutarse. Alternativa vía `gh api` (equivalente a los pasos de arriba), una vez que exista al menos un run de `backend`/`frontend`:

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
