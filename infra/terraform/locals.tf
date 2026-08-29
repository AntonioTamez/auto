# Única fuente de nombres y tags de todo recurso en main.tf. Ningún recurso
# hardcodea su propio nombre o tags fuera de aquí.
locals {
  common_tags = {
    project     = var.project_prefix
    environment = var.environment
    managed_by  = "terraform"
  }

  resource_group_name            = "rg-${var.project_prefix}-${var.environment}"
  container_app_environment_name = "cae-${var.project_prefix}-${var.environment}"
  log_analytics_workspace_name   = "log-${var.project_prefix}-${var.environment}"
  container_app_name             = "ca-${var.project_prefix}-${var.environment}"
  static_web_app_name            = "swa-${var.project_prefix}-${var.environment}"
  communication_service_name     = "acs-${var.project_prefix}-${var.environment}"

  # PostgreSQL Flexible Server y Storage Account exponen un FQDN/hostname
  # globalmente único en Azure; se les agrega un sufijo aleatorio (generado
  # una sola vez por ambiente, ver main.tf) para evitar colisiones de nombre
  # con otras suscripciones.
  postgresql_server_name = lower("psql-${var.project_prefix}-${var.environment}-${random_string.suffix.result}")

  # Storage account: 3-24 caracteres, solo minúsculas/números, sin guiones.
  storage_account_name = lower("st${var.project_prefix}${var.environment}${random_string.suffix.result}")
}
