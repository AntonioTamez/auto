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
