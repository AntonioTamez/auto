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
