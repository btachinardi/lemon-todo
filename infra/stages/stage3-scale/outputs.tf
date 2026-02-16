output "resource_group_name" {
  description = "Resource group name"
  value       = azurerm_resource_group.this.name
}

output "app_service_hostname" {
  description = "API hostname (direct)"
  value       = module.app_service.default_hostname
}

output "frontdoor_hostname" {
  description = "Front Door endpoint hostname"
  value       = module.frontdoor.endpoint_hostname
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

output "redis_hostname" {
  description = "Redis Cache hostname"
  value       = module.redis.hostname
}

output "cdn_endpoint" {
  description = "CDN storage endpoint"
  value       = module.cdn.primary_web_endpoint
}

output "key_vault_uri" {
  description = "Key Vault URI"
  value       = module.key_vault.key_vault_uri
}
