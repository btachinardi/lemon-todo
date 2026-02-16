output "app_service_id" {
  description = "Web App resource ID"
  value       = azurerm_linux_web_app.this.id
}

output "app_service_name" {
  description = "Web App name"
  value       = azurerm_linux_web_app.this.name
}

output "default_hostname" {
  description = "Default hostname of the web app"
  value       = azurerm_linux_web_app.this.default_hostname
}

output "principal_id" {
  description = "Managed identity principal ID"
  value       = azurerm_linux_web_app.this.identity[0].principal_id
}

output "service_plan_id" {
  description = "App Service Plan resource ID"
  value       = azurerm_service_plan.this.id
}
