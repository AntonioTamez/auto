---
title: 'Publicar el contrato OpenAPI'
type: 'feature'
created: '2026-09-04'
status: 'done'
review_loop_iteration: 1
baseline_commit: '8841a1fa23573cc99e1247077d31569c088449ee'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-auto-2026-08-19/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La spec OpenAPI ya se genera en runtime (`AddOpenApi()`/`MapOpenApi()`) pero solo se sirve en `Development` -- no existe ningún paso del pipeline que la genere y publique como artefacto versionado, así que Angular (y después Flutter) no tienen un contrato preciso contra el cual construir fuera de la máquina de un desarrollador.

**Approach:** Habilitar generación de la spec OpenAPI **en build-time** en `src/Api/Api.csproj` (paquete `Microsoft.Extensions.ApiDescription.Server` + `OpenApiGenerateDocumentsOnBuild`), reutilizando el paso `dotnet build` que `ci.yml` ya ejecuta, y agregar un step de `actions/upload-artifact` al job `backend` que publique el JSON generado nombrado con `${{ github.sha }}`, siguiendo el mismo esquema de versionado por commit SHA que ya usa `cd-dev.yml` para las imágenes Docker.

## Boundaries & Constraints

**Always:**
- Generación **build-time** (`OpenApiGenerateDocumentsOnBuild=true`) dentro del `dotnet build` que `ci.yml:29-30` ya corre -- ningún paso nuevo de build o de correr la app.
- El artefacto publicado se nombra incluyendo `${{ github.sha }}` (mismo esquema que `cd-dev.yml:69-71` para el tag Docker) -- versión = commit inmutable.
- Corre en el job `backend` existente de `ci.yml`, en ambos triggers ya configurados (`push` a cualquier rama y `pull_request`) -- sin trigger nuevo.
- `Microsoft.AspNetCore.OpenApi` (ya referenciado en `Api.csproj:4`) sigue siendo el único generador -- no se agrega Swashbuckle ni NSwag.

**Ask First:** Ninguno -- el AC de la historia pide explícitamente "artefacto versionado del pipeline", no un endpoint runtime público; el alcance de build-time + `upload-artifact` lo satisface sin decisiones adicionales.

**Never:**
- No cambia el gating de `Program.cs:39-42` (`MapOpenApi()` solo en `Development`) -- publicar es una preocupación de CI, no de qué se sirve en runtime.
- No crea contenedor de Blob Storage en Terraform para alojar la spec -- el artifact de GitHub Actions ya es "versionado y accesible" y cumple el AC.
- No agrega Problem Details, JWT, ni ninguna otra decisión de scaffold del épico ajena a esta historia.
- No toca `cd-dev.yml` ni `destroy-dev.yml`.
- No agrega generación de cliente HTTP en Angular (`ng-openapi-gen` u otro) -- fuera de alcance de esta historia.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Push o PR dispara CI | Cualquier rama/PR | `dotnet build` genera el JSON OpenAPI junto al output del build; step `upload-artifact` lo publica como `openapi-spec-{sha}` | N/A |
| `dotnet build` falla (error de compilación) | Código roto en `src/Api` o sus dependencias | No se genera el JSON OpenAPI (el build falla antes) | El job `backend` falla; no corre el step de upload |

</frozen-after-approval>

## Code Map

- `src/Api/Api.csproj:3-5` -- agregar `PackageReference` a `Microsoft.Extensions.ApiDescription.Server` (pineada a la MISMA versión exacta que `Microsoft.AspNetCore.OpenApi`, `10.0.9` -- versiones distintas chocan en runtime con `FileLoadException` por conflicto de `Microsoft.OpenApi.dll`) y las propiedades MSBuild `OpenApiDocumentsDirectory`/`OpenApiGenerateDocumentsOnBuild=true` en un nuevo `PropertyGroup`.
- `src/Api/Api.csproj` -- **`OpenApiDocumentsDirectory` NO debe apuntar a `$(MSBuildProjectDirectory)` (raíz del proyecto)**: el SDK `Microsoft.NET.Sdk.Web` incluye por defecto cualquier archivo suelto en la raíz del proyecto como contenido publicable, así que un JSON generado ahí también se copia a `dotnet publish` -- y por lo tanto a la imagen Docker de producción (`src/Api/Dockerfile`), no solo al artifact de CI. Usar en su lugar una ruta fija bajo `obj/`, ej. `$(BaseIntermediateOutputPath)openapi\` (produce `src/Api/obj/openapi/Api.json`): sigue siendo independiente de Debug/Release (a diferencia de `$(IntermediateOutputPath)`, que sí varía por configuración), y `obj/` ya está fuera de los globs de contenido publicable del SDK y ya cubierto por la entrada `obj/` existente en `.gitignore`.
- `src/Api/Dockerfile:35-38` -- step `dotnet publish src/Api/Api.csproj ... --output /app/publish`; `Dockerfile:50` -- `COPY --from=build /app/publish .` copia ese output a la imagen runtime. Verificar explícitamente (build local de la imagen o `dotnet publish` directo) que `Api.json` YA NO aparece en `/app/publish` tras mover `OpenApiDocumentsDirectory` fuera de la raíz del proyecto.
- `src/Api/Program.cs:5` -- `builder.Services.AddOpenApi();` ya wireado; confirma que Application/Api es la fuente real del documento (no tocar).
- `src/Api/Program.cs:39-42` -- `MapOpenApi()` gateado a `Development`; no se modifica en esta historia.
- `.github/workflows/ci.yml:29-30` -- step `dotnet build`; el JSON aparece en `src/Api/obj/openapi/Api.json` tras el cambio de csproj.
- `.github/workflows/ci.yml:32-33` -- step `dotnet test`, último step del job `backend` hoy; el nuevo `upload-artifact` se agrega después de este, con `path: src/Api/obj/openapi/Api.json`.
- `.github/workflows/cd-dev.yml:69-71` -- precedente de versionado por `${{ github.sha }}` a replicar en el nombre del artifact.
- `.gitignore` -- **no** agregar una entrada nueva: `obj/` ya cubre `src/Api/obj/openapi/Api.json`.

## Tasks & Acceptance

**Execution:**
- [x] `src/Api/Api.csproj` -- agregar `PackageReference Microsoft.Extensions.ApiDescription.Server` (versión `10.0.9`, igual a `Microsoft.AspNetCore.OpenApi`) + `OpenApiGenerateDocumentsOnBuild=true` + `OpenApiDocumentsDirectory=$(BaseIntermediateOutputPath)openapi\` (NO `$(MSBuildProjectDirectory)`) -- habilita generación build-time sin tocar Program.cs y sin exponer el JSON a `dotnet publish`
- [x] `.github/workflows/ci.yml` -- agregar step `actions/upload-artifact@v4` al job `backend`, después de `dotnet test`, subiendo `src/Api/obj/openapi/Api.json` con `name: openapi-spec-${{ github.sha }}` -- publica el contrato como artefacto versionado del pipeline
- [x] `tests/Api.Tests` (o el proyecto de test existente) -- verificar que el build genera el archivo esperado en `src/Api/obj/openapi/Api.json` localmente (`dotnet build` + assert de existencia y contenido del JSON) -- confirma que el wiring de csproj funciona antes de depender del step de CI
- [x] Verificación manual/local -- correr `dotnet publish src/Api/Api.csproj -c Release -o <dir>` (mismos flags que `src/Api/Dockerfile`) y confirmar que `Api.json` **NO** aparece en `<dir>` -- cierra el gap encontrado en la ronda de revisión anterior (ver Spec Change Log)

**Acceptance Criteria:**
- Given el endpoint `/health` ya expuesto, when se corre `ci.yml` (push o PR), then el job `backend` produce un archivo OpenAPI JSON durante `dotnet build` sin ejecutar la app.
- Given un run exitoso de `ci.yml`, when se inspecciona la pestaña Actions del run, then existe un artifact descargable nombrado con el `github.sha` del commit, conteniendo la spec OpenAPI generada desde `Application`/`Api`.
- Given dos runs de commits distintos, when se comparan sus artifacts, then cada uno queda versionado independientemente (nombres distintos, sin sobrescritura).
- Given el mismo `dotnet publish` que usa `src/Api/Dockerfile` para construir la imagen de producción, when se inspecciona el directorio de publish resultante, then `Api.json` no está presente -- el contrato solo llega al artifact de CI, nunca a la imagen desplegada.

## Spec Change Log

- **2026-09-04, review_loop_iteration 1** -- Finding (verification-gap, empíricamente reproducido: `dotnet publish src/Api/Api.csproj` con los mismos flags que `src/Api/Dockerfile` copió `Api.json` al directorio de publish): la primera implementación fijó `OpenApiDocumentsDirectory=$(MSBuildProjectDirectory)` para tener una ruta predecible -- pero esa ruta cae dentro de los globs de contenido publicable por defecto del SDK `Microsoft.NET.Sdk.Web`, así que el mismo JSON generado para el artifact de CI también terminaba copiado a `/app/publish` y de ahí a la imagen Docker de producción que despliega `cd-dev.yml`, sin que ningún test o step lo detectara. Amended: `OpenApiDocumentsDirectory` cambia a `$(BaseIntermediateOutputPath)openapi\` (→ `src/Api/obj/openapi/Api.json`) -- sigue siendo una ruta fija independiente de Debug/Release, pero vive bajo `obj/`, fuera de esos globs y ya cubierta por el `.gitignore` existente sin necesitar una entrada nueva. Code Map y Tasks ahora incluyen verificar explícitamente contra `dotnet publish`/`Dockerfile`. Avoids: que el contrato completo de la API se filtre, sin revisión, a la imagen que corre en producción. KEEP: el mecanismo de generación build-time en sí (`Microsoft.Extensions.ApiDescription.Server` + `OpenApiGenerateDocumentsOnBuild=true`), pinear `Microsoft.Extensions.ApiDescription.Server` a la misma versión exacta (`10.0.9`) que `Microsoft.AspNetCore.OpenApi` (evita `FileLoadException` por conflicto de ensamblados), el step `upload-artifact@v4` nombrado `openapi-spec-${{ github.sha }}` en el job `backend` después de `dotnet test`, y el test que valida que el build produce un JSON OpenAPI válido con `/health` -- todo eso sigue vigente, solo cambia la ruta del archivo generado.

## Design Notes

`Microsoft.Extensions.ApiDescription.Server` es el paquete oficial de generación build-time (complementa a `Microsoft.AspNetCore.OpenApi`, que ya cubre la generación runtime gateada a `Development`). Con `OpenApiGenerateDocumentsOnBuild=true` y `OpenApiDocumentsDirectory` apuntando a `$(BaseIntermediateOutputPath)openapi\`, `dotnet build Auto.slnx` (ya ejecutado en `ci.yml:29-30`) produce el JSON sin ningún paso adicional de arranque de la app -- coherente con "Always: ningún paso nuevo de build o de correr la app". Por defecto (sin fijar `OpenApiDocumentsDirectory`), el paquete ya escribe a `obj/`; esta historia solo fija un subdirectorio propio (`obj/openapi/`) para tener un nombre de archivo predecible para `upload-artifact`, sin usar la raíz del proyecto (ver Spec Change Log -- esa ruta sí se filtraba a `dotnet publish`).

## Verification

**Commands:**
- `dotnet build src/Api/Api.csproj -c Release` -- expected: genera `src/Api/obj/openapi/Api.json`.
- `dotnet publish src/Api/Api.csproj -c Release --no-restore -o <dir>` (mismos flags que `src/Api/Dockerfile`) -- expected: `<dir>` NO contiene `Api.json`.
- `gh run view --log` (tras push) -- expected: el step `upload-artifact` reporta el artifact `openapi-spec-<sha>` subido correctamente.

**Manual checks (if no CLI):**
- Descargar el artifact desde la pestaña Actions del run más reciente y confirmar que el JSON es una spec OpenAPI válida (`openapi: 3.x`, `paths./health` presente).

## Suggested Review Order

**Por qué `obj/` y no la raíz del proyecto**

- Entry point: generación build-time habilitada, con la ruta de salida fijada deliberadamente bajo `obj/` -- no en la raíz del proyecto -- para evitar que el SDK Web la trate como contenido publicable.
  [`Api.csproj:16`](../../src/Api/Api.csproj#L16)

- El pin de versión exacta entre los dos paquetes OpenAPI evita un `FileLoadException` en runtime por conflicto de ensamblados.
  [`Api.csproj:27`](../../src/Api/Api.csproj#L27)

**Publicación como artefacto de CI**

- Se sube después de `dotnet test`, no antes -- si el build/test falla, no se publica un contrato de un commit no verificado.
  [`ci.yml:40`](../../.github/workflows/ci.yml#L40)

- Nombrado por `github.sha` (mismo esquema que el tag Docker de `cd-dev.yml`) y `if-no-files-found: error` para fallar fuerte si el wiring del csproj se rompe.
  [`ci.yml:43`](../../.github/workflows/ci.yml#L43)

**Cobertura de test**

- Confirma que el build genera el JSON en la ruta esperada y que describe `/health`, sin depender del step de CI para descubrir una regresión.
  [`OpenApiSpecPublishingTests.cs:24`](../../tests/Api.Tests/OpenApiSpecPublishingTests.cs#L24)
