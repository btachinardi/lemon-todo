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

# --- Container App (replaces App Service — no VM quota needed) ---
module "container_app" {
  source = "../../modules/container-app"

  project             = local.project
  environment         = local.environment
  location            = var.location
  location_short      = var.location_short
  resource_group_name = azurerm_resource_group.this.name

  log_analytics_workspace_id = module.monitoring.log_analytics_workspace_id

  sql_connection_string          = module.sql_database.connection_string
  jwt_secret_key                 = var.jwt_secret_key
  encryption_key                 = var.encryption_key
  app_insights_connection_string = module.monitoring.app_insights_connection_string
  cors_origin                    = "https://${module.static_web_app.default_hostname}"

  cpu          = 0.25
  memory       = "0.5Gi"
  max_replicas = 3

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
