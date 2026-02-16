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

# --- App Service (Premium auto-scale) ---
module "app_service" {
  source = "../../modules/app-service"

  project             = local.project
  environment         = local.environment
  location            = var.location
  location_short      = var.location_short
  resource_group_name = azurerm_resource_group.this.name

  sku_name             = "P2v3"
  always_on            = true
  enable_staging_slot  = true
  key_vault_id         = module.key_vault.key_vault_id

  sql_connection_string          = module.sql_database.connection_string
  app_insights_connection_string = module.monitoring.app_insights_connection_string

  extra_app_settings = {
    "Jwt__Issuer"                     = "LemonDo"
    "Jwt__Audience"                   = "LemonDo"
    "Jwt__SecretKey"                  = "@Microsoft.KeyVault(VaultName=${module.key_vault.key_vault_name};SecretName=jwt-secret-key)"
    "Encryption__FieldEncryptionKey"  = "@Microsoft.KeyVault(VaultName=${module.key_vault.key_vault_name};SecretName=field-encryption-key)"
    "Cors__AllowedOrigins__0"         = "https://${module.frontdoor.endpoint_hostname}"
    "ConnectionStrings__Redis"        = module.redis.connection_string
  }

  tags = local.tags
}

# --- Auto-scale for App Service ---
resource "azurerm_monitor_autoscale_setting" "app" {
  name                = "autoscale-${local.project}-${local.environment}"
  resource_group_name = azurerm_resource_group.this.name
  location            = var.location
  target_resource_id  = module.app_service.service_plan_id

  profile {
    name = "default"

    capacity {
      default = 2
      minimum = 2
      maximum = 10
    }

    rule {
      metric_trigger {
        metric_name        = "CpuPercentage"
        metric_resource_id = module.app_service.service_plan_id
        time_grain         = "PT1M"
        statistic          = "Average"
        time_window        = "PT5M"
        time_aggregation   = "Average"
        operator           = "GreaterThan"
        threshold          = 70
      }
      scale_action {
        direction = "Increase"
        type      = "ChangeCount"
        value     = "1"
        cooldown  = "PT5M"
      }
    }

    rule {
      metric_trigger {
        metric_name        = "CpuPercentage"
        metric_resource_id = module.app_service.service_plan_id
        time_grain         = "PT1M"
        statistic          = "Average"
        time_window        = "PT10M"
        time_aggregation   = "Average"
        operator           = "LessThan"
        threshold          = 25
      }
      scale_action {
        direction = "Decrease"
        type      = "ChangeCount"
        value     = "1"
        cooldown  = "PT10M"
      }
    }
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
  app_service_id              = module.app_service.app_service_id

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
  app_service_hostname = module.app_service.default_hostname
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
