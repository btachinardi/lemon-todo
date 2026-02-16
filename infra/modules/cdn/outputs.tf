output "storage_account_name" {
  description = "Storage account name for static content"
  value       = azurerm_storage_account.static.name
}

output "primary_web_endpoint" {
  description = "Primary web endpoint for static content"
  value       = azurerm_storage_account.static.primary_web_endpoint
}

output "primary_web_host" {
  description = "Primary web host for static content"
  value       = azurerm_storage_account.static.primary_web_host
}
