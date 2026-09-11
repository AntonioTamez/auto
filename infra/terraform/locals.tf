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

  # Historia 1.8 (monitoreo básico de disponibilidad).
  application_insights_name = "appi-${var.project_prefix}-${var.environment}"
  action_group_name         = "ag-${var.project_prefix}-${var.environment}"

  # Standard Web Tests (ping HTTP) de disponibilidad -- uno por target (API
  # y frontend). No se usa un solo web test genérico porque cada uno cubre
  # una URL distinta (ver spec 1.8 Code Map).
  availability_web_test_api_name      = "webtest-api-${var.project_prefix}-${var.environment}"
  availability_web_test_frontend_name = "webtest-frontend-${var.project_prefix}-${var.environment}"

  # Dos reglas de alerta (una por Standard Web Test) en vez de una sola
  # sobre la métrica agregada del componente -- la métrica agregada
  # promedia ambos targets y puede enmascarar la caída total de uno solo
  # (ver spec 1.8 Design Notes / Spec Change Log).
  availability_alert_api_name      = "alert-availability-api-${var.project_prefix}-${var.environment}"
  availability_alert_frontend_name = "alert-availability-frontend-${var.project_prefix}-${var.environment}"

  # 2 geo_locations fijas: failed_location_count = 2 en los metric alerts
  # exige que AMBAS reporten falla antes de notificar -- evita que un blip
  # transitorio de una sola región dispare una alerta (ver spec 1.8 Design
  # Notes).
  availability_web_test_geo_locations = ["us-tx-sn1-azr", "us-il-ch1-azr"]
}
