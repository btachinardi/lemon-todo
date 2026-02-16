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

output "container_app_id" {
  description = "Container App resource ID"
  value       = azurerm_container_app.this.id
}

output "environment_id" {
  description = "Container App Environment resource ID"
  value       = azurerm_container_app_environment.this.id
}

output "environment_custom_domain_verification_id" {
  description = "Custom domain verification ID for DNS TXT records"
  value       = azurerm_container_app_environment.this.custom_domain_verification_id
}

output "ingress_fqdn" {
  description = "Container App ingress FQDN (for DNS CNAME records)"
  value       = azurerm_container_app.this.ingress[0].fqdn
}
