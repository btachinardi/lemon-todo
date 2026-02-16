output "resource_group_name" {
  description = "Resource group name"
  value       = azurerm_resource_group.this.name
}

output "app_service_hostname" {
  description = "API hostname"
  value       = module.app_service.default_hostname
}

output "static_web_app_hostname" {
  description = "Frontend hostname"
  value       = module.static_web_app.default_hostname
}

output "static_web_app_api_key" {
  description = "Static Web App deployment key"
  value       = module.static_web_app.api_key
  sensitive   = true
}

output "sql_server_fqdn" {
  description = "SQL Server FQDN"
  value       = module.sql_database.server_fqdn
}

output "key_vault_uri" {
  description = "Key Vault URI"
  value       = module.key_vault.key_vault_uri
}

output "app_insights_connection_string" {
  description = "App Insights connection string"
  value       = module.monitoring.app_insights_connection_string
  sensitive   = true
}
