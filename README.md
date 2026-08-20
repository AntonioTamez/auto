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

Backend (por proyecto, desde la raíz del repo):

```
dotnet build src/Domain/Domain.csproj
dotnet build src/Application/Application.csproj
dotnet build src/Infrastructure/Infrastructure.csproj
dotnet build src/Api/Api.csproj
```

Frontend (desde `web/`):

```
npm run build
```
