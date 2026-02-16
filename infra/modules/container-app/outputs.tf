output "app_fqdn" {
  description = "Fully qualified domain name of the container app"
  value       = azurerm_container_app.this.latest_revision_fqdn
}

output "app_url" {
  description = "HTTPS URL of the container app"
  value       = "https://${azurerm_container_app.this.ingress[0].fqdn}"
}

output "container_registry_login_server" {
  description = "Container Registry login server URL"
  value       = azurerm_container_registry.this.login_server
}

output "container_registry_name" {
  description = "Container Registry name"
  value       = azurerm_container_registry.this.name
}

output "container_registry_admin_username" {
  description = "Container Registry admin username"
  value       = azurerm_container_registry.this.admin_username
}

output "container_registry_admin_password" {
  description = "Container Registry admin password"
  value       = azurerm_container_registry.this.admin_password
  sensitive   = true
}

output "container_app_name" {
  description = "Container App name"
  value       = azurerm_container_app.this.name
}
