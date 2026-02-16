resource "azurerm_mssql_server" "this" {
  name                         = "sql-${var.project}-${var.environment}-${var.location_short}"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.admin_login
  administrator_login_password = var.admin_password
  minimum_tls_version          = "1.2"

  azuread_administrator {
    login_username = var.aad_admin_login
    object_id      = var.aad_admin_object_id
  }

  tags = var.tags
}

resource "azurerm_mssql_database" "this" {
  name      = "sqldb-${var.project}-${var.environment}"
  server_id = azurerm_mssql_server.this.id

  sku_name                    = var.sku_name
  max_size_gb                 = var.max_size_gb
  zone_redundant              = var.zone_redundant
  geo_backup_enabled          = var.geo_backup_enabled
  read_scale                  = var.read_scale
  storage_account_type        = var.backup_storage_redundancy

  short_term_retention_policy {
    retention_days = var.short_term_retention_days
  }

  tags = var.tags
}

# Allow Azure services to connect (for App Service)
resource "azurerm_mssql_firewall_rule" "allow_azure" {
  count = var.allow_azure_services ? 1 : 0

  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.this.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}
