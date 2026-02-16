resource "azurerm_resource_group" "this" {
  name     = "rg-${local.project}-${local.environment}-${var.location_short}"
  location = var.location
  tags     = local.tags
}

# --- Monitoring ---
module "monitoring" {
  source = "../../modules/monitoring"

  project             = local.project
  environment         = local.environment
  location            = var.location
  location_short      = var.location_short
  resource_group_name = azurerm_resource_group.this.name
  retention_days      = 30
  tags                = local.tags
}

# --- Key Vault ---
module "key_vault" {
  source = "../../modules/key-vault"

  project             = local.project
  environment         = local.environment
  location            = var.location
  location_short      = var.location_short
  resource_group_name = azurerm_resource_group.this.name

  secrets = {
    "jwt-secret-key"       = var.jwt_secret_key
    "field-encryption-key" = var.encryption_key
  }

  tags = local.tags
}

# --- SQL Database ---
module "sql_database" {
  source = "../../modules/sql-database"

  project             = local.project
  environment         = local.environment
  location            = var.location
  location_short      = var.location_short
  resource_group_name = azurerm_resource_group.this.name

  admin_login         = var.sql_admin_login
  admin_password      = var.sql_admin_password
  aad_admin_login     = var.aad_admin_login
  aad_admin_object_id = var.aad_admin_object_id

  sku_name   = "Basic"
  max_size_gb = 2

  tags = local.tags
}

# --- App Service ---
module "app_service" {
  source = "../../modules/app-service"

  project             = local.project
  environment         = local.environment
  location            = var.location
  location_short      = var.location_short
  resource_group_name = azurerm_resource_group.this.name

  sku_name    = "B1"
  always_on   = true
  key_vault_id = module.key_vault.key_vault_id

  sql_connection_string          = module.sql_database.connection_string
  app_insights_connection_string = module.monitoring.app_insights_connection_string

  extra_app_settings = {
    "Jwt__Issuer"                   = "LemonDo"
    "Jwt__Audience"                 = "LemonDo"
    "Jwt__SecretKey"                = "@Microsoft.KeyVault(VaultName=${module.key_vault.key_vault_name};SecretName=jwt-secret-key)"
    "Encryption__FieldEncryptionKey" = "@Microsoft.KeyVault(VaultName=${module.key_vault.key_vault_name};SecretName=field-encryption-key)"
    "Cors__AllowedOrigins__0"       = "https://${module.static_web_app.default_hostname}"
  }

  tags = local.tags
}

# --- Static Web App ---
module "static_web_app" {
  source = "../../modules/static-web-app"

  project             = local.project
  environment         = local.environment
  location            = var.location
  location_short      = var.location_short
  resource_group_name = azurerm_resource_group.this.name

  sku_tier = "Free"
  tags     = local.tags
}
