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
  retention_days      = 90
  tags                = local.tags
}

# --- Key Vault ---
module "key_vault" {
  source = "../../modules/key-vault"

  project                  = local.project
  environment              = local.environment
  location                 = var.location
  location_short           = var.location_short
  resource_group_name      = azurerm_resource_group.this.name
  purge_protection_enabled = true

  secrets = {
    "jwt-secret-key"       = var.jwt_secret_key
    "field-encryption-key" = var.encryption_key
  }

  tags = local.tags
}

# --- SQL Database (Premium + read replica) ---
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

  sku_name                  = "P1"
  max_size_gb               = 500
  zone_redundant            = true
  geo_backup_enabled        = true
  read_scale                = true
  backup_storage_redundancy = "GeoZone"
  short_term_retention_days = 35

  tags = local.tags
}

# --- Container App (Premium — replaces App Service + auto-scale) ---
# Container Apps have built-in auto-scaling (min/max replicas + HTTP/CPU rules)
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
  cors_origin                    = "https://${module.frontdoor.endpoint_hostname}"

  cpu          = 1.0
  memory       = "2Gi"
  max_replicas = 10

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

  sku_tier = "Standard"
  tags     = local.tags
}

# --- Networking ---
module "networking" {
  source = "../../modules/networking"

  project             = local.project
  environment         = local.environment
  location            = var.location
  location_short      = var.location_short
  resource_group_name = azurerm_resource_group.this.name

  enable_sql_private_endpoint = true
  sql_server_id               = module.sql_database.server_id
  # TODO: Update networking module to accept container_app_environment_id
  app_service_id              = "" # Placeholder — Container Apps use VNet integration differently

  tags = local.tags
}

# --- Front Door (Premium + WAF) ---
module "frontdoor" {
  source = "../../modules/frontdoor"

  project             = local.project
  environment         = local.environment
  location_short      = var.location_short
  resource_group_name = azurerm_resource_group.this.name

  sku_name             = "Premium_AzureFrontDoor"
  app_service_hostname = module.container_app.app_fqdn
  enable_waf           = true
  waf_mode             = "Prevention"

  tags = local.tags
}

# --- CDN + Storage for static assets ---
module "cdn" {
  source = "../../modules/cdn"

  project              = local.project
  environment          = local.environment
  location             = var.location
  resource_group_name  = azurerm_resource_group.this.name
  frontdoor_profile_id = module.frontdoor.profile_id

  storage_replication_type = "ZRS"
  allowed_origins          = ["https://${module.frontdoor.endpoint_hostname}"]

  tags = local.tags
}

# --- Redis Cache ---
module "redis" {
  source = "../../modules/redis"

  project             = local.project
  environment         = local.environment
  location            = var.location
  location_short      = var.location_short
  resource_group_name = azurerm_resource_group.this.name

  sku_name     = "Premium"
  family       = "P"
  capacity     = 1
  pe_subnet_id = module.networking.pe_subnet_id

  tags = local.tags
}
