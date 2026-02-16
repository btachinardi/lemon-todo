output "server_id" {
  description = "SQL Server resource ID"
  value       = azurerm_mssql_server.this.id
}

output "server_fqdn" {
  description = "SQL Server fully qualified domain name"
  value       = azurerm_mssql_server.this.fully_qualified_domain_name
}

output "database_id" {
  description = "SQL Database resource ID"
  value       = azurerm_mssql_database.this.id
}

output "database_name" {
  description = "SQL Database name"
  value       = azurerm_mssql_database.this.name
}

output "connection_string" {
  description = "ADO.NET connection string"
  value       = "Server=tcp:${azurerm_mssql_server.this.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.this.name};Persist Security Info=False;User ID=${var.admin_login};Password=${var.admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  sensitive   = true
}
