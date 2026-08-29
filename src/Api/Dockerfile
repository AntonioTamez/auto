# Build de la imagen de la API (.NET 10 / ASP.NET Core), consumida por
# Azure Container Apps (historia 1.4, CD a dev). Contexto de build = raíz
# del repositorio (no src/Api/) porque el Dockerfile hace `COPY src/...`
# para poder restaurar los ProjectReference de Clean Architecture
# (Api -> Application -> Domain). Construir con:
#
#   docker build -f src/Api/Dockerfile .
#
# desde la raíz del repo. Ver .dockerignore (raíz) para lo que se excluye
# del contexto de build.

# ---- Etapa build: restore + publish sobre el SDK completo -----------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# global.json fija el SDK exacto (10.0.301) usado también en CI (historia 1.3).
COPY global.json ./
COPY src/Directory.Build.props src/Directory.Build.props

# Copia solo los .csproj primero (capa de restore cacheable independiente
# del código fuente) -- la cadena de ProjectReference que Api necesita en
# tiempo de compilación hoy es Api -> Application -> Domain. Infrastructure
# todavía no es referenciado por Api (ver deferred-work.md), así que no
# participa del restore.
COPY src/Api/Api.csproj src/Api/Api.csproj
COPY src/Application/Application.csproj src/Application/Application.csproj
COPY src/Domain/Domain.csproj src/Domain/Domain.csproj

RUN dotnet restore src/Api/Api.csproj

# Ahora sí el resto del código fuente (tests/, infra/, web/, etc. ya están
# excluidos por .dockerignore, así que este COPY solo trae src/).
COPY src/ src/

RUN dotnet publish src/Api/Api.csproj \
    --no-restore \
    --configuration Release \
    --output /app/publish

# ---- Etapa runtime: solo el runtime de ASP.NET Core, sin SDK --------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Azure Container Apps enruta tráfico externo (HTTPS) hacia el container
# sobre HTTP en target_port 8080 (infra/terraform/main.tf,
# azurerm_container_app.api) -- debe coincidir exactamente con este puerto.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Api.dll"]
