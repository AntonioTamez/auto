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
