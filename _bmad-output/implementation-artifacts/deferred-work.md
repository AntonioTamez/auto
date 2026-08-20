- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-esqueleto-del-repositorio-clean-architecture-angular.md`
  summary: Decidir explícitamente si `src/Api` necesita una referencia directa a `src/Infrastructure` para poder componer sus implementaciones concretas en el contenedor de DI.
  evidence: Hoy `Api.csproj` solo referencia `Application` (tal como definía el Code Map de la historia 1.1). Como `Infrastructure` todavía está vacío, esto no rompe nada aún, pero el propio Architecture Spine dice que `Api` "compone DI e integra Infrastructure" sin declarar explícitamente esa dependencia de proyecto. Revisar en cuanto `Infrastructure` tenga su primera implementación real (EF Core, repos, etc.).

- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-esqueleto-del-repositorio-clean-architecture-angular.md`
  summary: `Microsoft.AspNetCore.OpenApi 10.0.9` arrastra `Microsoft.OpenApi 2.0.0`, marcado por NuGet como advisory de severidad alta (NU1903, GHSA-v5pm-xwqc-g5wc).
  evidence: `dotnet build` sobre `src/Api` compila con 0 errores pero 2 warnings por este advisory. Es el default del template `dotnet new webapi`, no una elección de esta historia. Hoy no hay superficie de ataque real (no se expone ningún schema OpenAPI todavía), pero conviene resolverlo (actualizar/pinnear versión) antes de que la historia 1.7 publique el contrato OpenAPI real.
