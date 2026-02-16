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
  cors_origin                    = var.frontend_custom_domain != "" ? "https://${var.frontend_custom_domain}" : "https://${module.static_web_app.default_hostname}"
  cors_origin_secondary          = var.frontend_custom_domain != "" ? "https://${module.static_web_app.default_hostname}" : ""

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

  sku_tier = "Standard"
  tags     = local.tags
}

# --- Custom Domains (set variables after DNS records are created) ---

# Container App: custom domain + managed TLS certificate (via AzAPI)
# Step 1: Add domain with binding disabled (certificate comes next)
resource "azapi_update_resource" "api_custom_domain" {
  count       = var.api_custom_domain != "" ? 1 : 0
  type        = "Microsoft.App/containerApps@2024-03-01"
  resource_id = module.container_app.container_app_id

  body = {
    properties = {
      configuration = {
        ingress = {
          customDomains = [
            {
              bindingType = "Disabled"
              name        = var.api_custom_domain
            }
          ]
        }
      }
    }
  }
}

# Step 2: Create managed certificate (requires DNS validation to pass)
resource "azapi_resource" "api_managed_certificate" {
  count     = var.api_custom_domain != "" ? 1 : 0
  type      = "Microsoft.App/managedEnvironments/managedCertificates@2024-03-01"
  name      = "${local.project}-${local.environment}-api-cert"
  parent_id = module.container_app.environment_id
  location  = var.location

  body = {
    properties = {
      subjectName             = var.api_custom_domain
      domainControlValidation = "CNAME"
    }
  }

  depends_on = [azapi_update_resource.api_custom_domain]
}

# Step 3: Bind the managed certificate to the custom domain
resource "azapi_update_resource" "api_custom_domain_binding" {
  count       = var.api_custom_domain != "" ? 1 : 0
  type        = "Microsoft.App/containerApps@2024-03-01"
  resource_id = module.container_app.container_app_id

  body = {
    properties = {
      configuration = {
        ingress = {
          customDomains = [
            {
              bindingType   = "SniEnabled"
              name          = var.api_custom_domain
              certificateId = azapi_resource.api_managed_certificate[0].id
            }
          ]
        }
      }
    }
  }

  depends_on = [azapi_resource.api_managed_certificate]
}

# Static Web App: custom domain (CNAME delegation — Azure handles SSL automatically)
resource "azurerm_static_web_app_custom_domain" "frontend" {
  count             = var.frontend_custom_domain != "" ? 1 : 0
  static_web_app_id = module.static_web_app.static_web_app_id
  domain_name       = var.frontend_custom_domain
  validation_type   = "cname-delegation"
}
