# Sufijo aleatorio de 4 caracteres, estable por ambiente (no se regenera en
# cada apply), usado solo donde Azure exige nombre globalmente único
# (PostgreSQL Flexible Server, Storage Account). Ver locals.tf.
resource "random_string" "suffix" {
  length  = 4
  special = false
  upper   = false
  numeric = true
}

# Password administrador de PostgreSQL: generada por Terraform, nunca
# hardcodeada en código versionado (ver Consistency Conventions del spine:
# "no secrets in source"). Expuesta como output sensible para que la
# historia 1.4 (CD) la consuma.
resource "random_password" "postgres_admin" {
  length           = 24
  special          = true
  override_special = "-_"
  min_upper        = 1
  min_lower        = 1
  min_numeric      = 1
  min_special      = 1
}

resource "azurerm_resource_group" "main" {
  name     = local.resource_group_name
  location = var.location
  tags     = local.common_tags
}

# azurerm ~> 4.0 exige log_analytics_workspace_id en la Environment (no es
# opcional en esta major, a diferencia de versiones futuras del provider) --
# gap de la historia 1.2 que cierra esta historia (1.4).
resource "azurerm_log_analytics_workspace" "main" {
  name                = local.log_analytics_workspace_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = local.common_tags
}

# Workspace-based (no clásico): apunta al Log Analytics Workspace ya
# existente en vez de crear un segundo workspace (historia 1.8, Boundaries
# & Constraints).
resource "azurerm_application_insights" "main" {
  name                = local.application_insights_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"
  tags                = local.common_tags
}

resource "azurerm_container_app_environment" "main" {
  name                       = local.container_app_environment_name
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
  tags                       = local.common_tags
}

# Container App de la API. Consumption plan (sin workload_profile block),
# min_replicas = 0 (scale-to-zero, ver spine AD-12 / Design Notes de esta
# historia). Sin bloque `registry`: la imagen (ghcr.io/antoniotamez/auto-api)
# es pública, no requiere credenciales de pull.
resource "azurerm_container_app" "api" {
  name                         = local.container_app_name
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"
  tags                         = local.common_tags

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "api"
      image  = var.api_container_image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      # Origen permitido para CORS (Program.cs lee Cors:AllowedOrigins vía
      # configuración) -- el host real de la SWA, conocido recién en este
      # apply. Nunca AllowAnyOrigin en el ambiente desplegado (spec 1.5).
      env {
        name  = "Cors__AllowedOrigins__0"
        value = "https://${azurerm_static_web_app.main.default_host_name}"
      }

      # Conecta la API a Application Insights (historia 1.8) -- mismo
      # patrón que Cors__AllowedOrigins__0 de arriba, nunca hardcodeada.
      # Program.cs lee esta variable vía builder.Configuration y solo
      # registra OpenTelemetry si viene no vacía.
      env {
        name  = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        value = azurerm_application_insights.main.connection_string
      }

      # Cierra el gap diferido explícitamente por la historia 1.4 ("sin
      # probe... deferred a la historia 1.5"). Mismo puerto/path que el
      # endpoint /health de Program.cs.
      liveness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/health"
      }

      readiness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/health"
      }
    }
  }

  # target_port 8080 coincide con ASPNETCORE_HTTP_PORTS en src/Api/Dockerfile.
  ingress {
    external_enabled = true
    target_port      = 8080

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }
}

resource "azurerm_static_web_app" "main" {
  name                = local.static_web_app_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku_tier            = "Standard"
  sku_size            = "Standard"
  tags                = local.common_tags
}

# Standard Web Test (ping HTTP) contra /health -- mide disponibilidad real
# de la API sin depender de que Application Insights ya esté cableado en
# el proceso (historia 1.8, Boundaries & Constraints / Design Notes).
resource "azurerm_application_insights_standard_web_test" "api" {
  name                    = local.availability_web_test_api_name
  resource_group_name     = azurerm_resource_group.main.name
  location                = azurerm_resource_group.main.location
  application_insights_id = azurerm_application_insights.main.id
  geo_locations           = local.availability_web_test_geo_locations
  enabled                 = true
  tags                    = local.common_tags

  request {
    url = "https://${azurerm_container_app.api.ingress[0].fqdn}/health"
  }
}

# Standard Web Test (ping HTTP) contra la Static Web App -- mide
# disponibilidad del frontend sin agregar ningún SDK de telemetría
# cliente-side en Angular (historia 1.8, Boundaries & Constraints / Never).
resource "azurerm_application_insights_standard_web_test" "frontend" {
  name                    = local.availability_web_test_frontend_name
  resource_group_name     = azurerm_resource_group.main.name
  location                = azurerm_resource_group.main.location
  application_insights_id = azurerm_application_insights.main.id
  geo_locations           = local.availability_web_test_geo_locations
  enabled                 = true
  tags                    = local.common_tags

  request {
    url = "https://${azurerm_static_web_app.main.default_host_name}"
  }
}

# Único canal de notificación operativo del repo (Communication Services
# existente es para OTP de usuarios, Epic 5, no para alertas -- historia
# 1.8, Ask First).
resource "azurerm_monitor_action_group" "main" {
  name                = local.action_group_name
  resource_group_name = azurerm_resource_group.main.name
  short_name          = "availability"
  tags                = local.common_tags

  email_receiver {
    name          = "availability-alert-email"
    email_address = var.availability_alert_email
  }
}

# Dos reglas de alerta (una por Standard Web Test), cada una con el bloque
# dedicado application_insights_web_test_location_availability_criteria --
# NO un criteria genérico sobre la métrica agregada del componente, que
# promedia ambos targets y puede enmascarar la caída total de uno solo
# (historia 1.8, Code Map / Design Notes / Spec Change Log).
resource "azurerm_monitor_metric_alert" "availability_api" {
  name                = local.availability_alert_api_name
  resource_group_name = azurerm_resource_group.main.name
  description         = "Notifica cuando el Standard Web Test de la API (/health) falla en las 2 ubicaciones geográficas configuradas (NFR-4, 99% uptime)."
  scopes = [
    azurerm_application_insights.main.id,
    azurerm_application_insights_standard_web_test.api.id,
  ]

  application_insights_web_test_location_availability_criteria {
    web_test_id           = azurerm_application_insights_standard_web_test.api.id
    component_id          = azurerm_application_insights.main.id
    failed_location_count = length(local.availability_web_test_geo_locations)
  }

  action {
    action_group_id = azurerm_monitor_action_group.main.id
  }

  tags = local.common_tags
}

resource "azurerm_monitor_metric_alert" "availability_frontend" {
  name                = local.availability_alert_frontend_name
  resource_group_name = azurerm_resource_group.main.name
  description         = "Notifica cuando el Standard Web Test del frontend (Static Web App) falla en las 2 ubicaciones geográficas configuradas (NFR-4, 99% uptime)."
  scopes = [
    azurerm_application_insights.main.id,
    azurerm_application_insights_standard_web_test.frontend.id,
  ]

  application_insights_web_test_location_availability_criteria {
    web_test_id           = azurerm_application_insights_standard_web_test.frontend.id
    component_id          = azurerm_application_insights.main.id
    failed_location_count = length(local.availability_web_test_geo_locations)
  }

  action {
    action_group_id = azurerm_monitor_action_group.main.id
  }

  tags = local.common_tags
}

resource "azurerm_postgresql_flexible_server" "main" {
  name                   = local.postgresql_server_name
  location               = azurerm_resource_group.main.location
  resource_group_name    = azurerm_resource_group.main.name
  version                = "16"
  administrator_login    = "psqladmin"
  administrator_password = random_password.postgres_admin.result
  storage_mb             = 32768
  sku_name               = "B_Standard_B1ms"
  tags                   = local.common_tags

  # Un cambio a project_prefix/environment (o drift del sufijo aleatorio)
  # cambia el nombre y fuerza un destroy+recreate; esto protege los datos
  # reales de una recreación accidental (review de código, historia 1.2).
  #
  # ignore_changes = [zone]: Azure asigna la zona de disponibilidad
  # automáticamente al crear el servidor (no la fijamos en config); sin este
  # ignore, Terraform intenta "corregir" ese valor computado en applies
  # subsecuentes y Azure lo rechaza (zone solo puede cambiar intercambiando
  # con high_availability.standby_availability_zone) -- descubierto en el
  # primer apply real, historia 1.4.
  lifecycle {
    prevent_destroy = true
    ignore_changes  = [zone]
  }
}

resource "azurerm_storage_account" "main" {
  name                            = local.storage_account_name
  location                        = azurerm_resource_group.main.location
  resource_group_name             = azurerm_resource_group.main.name
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  account_kind                    = "StorageV2"
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  tags                            = local.common_tags

  # Mismo riesgo que el Postgres Flexible Server de arriba: un cambio a
  # project_prefix/environment fuerza un destroy+recreate de este nombre
  # globalmente único, destruyendo blobs reales (review de código, historia 1.2).
  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_communication_service" "main" {
  name                = local.communication_service_name
  resource_group_name = azurerm_resource_group.main.name
  data_location       = "United States" # Mexico no está entre los data_location soportados por ACS; el más cercano es United States.
  tags                = local.common_tags
}
