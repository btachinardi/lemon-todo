output "vnet_id" {
  description = "Virtual network resource ID"
  value       = azurerm_virtual_network.this.id
}

output "app_subnet_id" {
  description = "App Service subnet ID"
  value       = azurerm_subnet.app.id
}

output "pe_subnet_id" {
  description = "Private endpoints subnet ID"
  value       = azurerm_subnet.private_endpoints.id
}
