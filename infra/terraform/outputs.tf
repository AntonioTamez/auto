output "resource_group_name" {
  description = "Nombre del resource group que agrupa todos los recursos de este ambiente."
  value       = azurerm_resource_group.main.name
}

output "container_app_environment_id" {
  description = "ID del Container Apps Environment donde la historia 1.4 desplegará el Container App de la API."
  value       = azurerm_container_app_environment.main.id
}

output "container_app_environment_default_domain" {
  description = "Dominio por defecto del Container Apps Environment."
  value       = azurerm_container_app_environment.main.default_domain
}

output "container_app_fqdn" {
  description = "FQDN público del Container App de la API -- lo necesita la historia 1.5 para el health-check end-to-end."
  value       = azurerm_container_app.api.ingress[0].fqdn
}

output "static_web_app_id" {
  description = "ID de la Static Web App donde la historia 1.4 desplegará el build de Angular."
  value       = azurerm_static_web_app.main.id
}

output "static_web_app_default_host_name" {
  description = "Hostname público por defecto de la Static Web App."
  value       = azurerm_static_web_app.main.default_host_name
}

output "static_web_app_api_key" {
  description = "API key de despliegue de la Static Web App, usada por el pipeline de CD (ej. Azure/static-web-apps-deploy-action)."
  value       = azurerm_static_web_app.main.api_key
  sensitive   = true
}

output "postgresql_server_fqdn" {
  description = "FQDN del PostgreSQL Flexible Server, para construir la connection string de la API."
  value       = azurerm_postgresql_flexible_server.main.fqdn
}

output "postgresql_administrator_login" {
  description = "Login administrador de PostgreSQL."
  value       = azurerm_postgresql_flexible_server.main.administrator_login
}

output "postgresql_administrator_password" {
  description = "Password administrador de PostgreSQL, generada por Terraform (random_password) — nunca hardcodeada en código."
  value       = random_password.postgres_admin.result
  sensitive   = true
}

output "storage_account_name" {
  description = "Nombre de la Storage Account (Blob Storage) usada para imágenes de vehículos."
  value       = azurerm_storage_account.main.name
}

output "storage_account_primary_blob_endpoint" {
  description = "Endpoint primario de Blob Storage."
  value       = azurerm_storage_account.main.primary_blob_endpoint
}

output "communication_service_name" {
  description = "Nombre del Communication Service (email OTP, spine AD-4)."
  value       = azurerm_communication_service.main.name
}
