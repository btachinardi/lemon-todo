output "resource_group_name" {
  description = "Resource group name"
  value       = azurerm_resource_group.this.name
}

output "api_url" {
  description = "API URL (Container App)"
  value       = module.container_app.app_url
}

output "api_fqdn" {
  description = "API FQDN (Container App)"
  value       = module.container_app.app_fqdn
}

output "container_registry_login_server" {
  description = "Container Registry login server"
  value       = module.container_app.container_registry_login_server
}

output "container_registry_name" {
  description = "Container Registry name"
  value       = module.container_app.container_registry_name
}

output "container_app_name" {
  description = "Container App name"
  value       = module.container_app.container_app_name
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
