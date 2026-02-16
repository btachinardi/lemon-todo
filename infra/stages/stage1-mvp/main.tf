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

# Container App: custom domain + managed TLS certificate (via Azure CLI)
# azapi_update_resource does a full PUT which fails because it requires all secrets
# in the body. Azure CLI commands are the recommended approach for Container App
# custom domains with managed certificates.
resource "terraform_data" "api_custom_domain" {
  count = var.api_custom_domain != "" ? 1 : 0

  input = {
    hostname     = var.api_custom_domain
    app_name     = module.container_app.container_app_name
    env_name     = "cae-${local.project}-${local.environment}-${var.location_short}"
    rg_name      = azurerm_resource_group.this.name
  }

  provisioner "local-exec" {
    interpreter = ["bash", "-c"]
    command = <<-EOT
      set -e

      # Step 1: Add hostname (idempotent — ignore "already exists" error)
      az containerapp hostname add \
        --hostname "${self.input.hostname}" \
        --name "${self.input.app_name}" \
        --resource-group "${self.input.rg_name}" 2>&1 || true

      # Step 2: Create managed certificate (idempotent — ignore "already exists" error)
      az containerapp env certificate create \
        --hostname "${self.input.hostname}" \
        --name "${self.input.env_name}" \
        --resource-group "${self.input.rg_name}" \
        --validation-method CNAME 2>&1 || true

      # Step 3: Wait for certificate to reach Succeeded state (up to 5 minutes)
      echo "Waiting for managed certificate to provision..."
      for i in $(seq 1 30); do
        STATE=$(az containerapp env certificate list \
          --name "${self.input.env_name}" \
          --resource-group "${self.input.rg_name}" \
          --query "[?properties.subjectName=='${self.input.hostname}'].properties.provisioningState | [0]" -o tsv 2>/dev/null)
        if [ "$STATE" = "Succeeded" ]; then
          echo "Certificate provisioned successfully."
          break
        fi
        echo "  Certificate state: $STATE (attempt $i/30, waiting 10s...)"
        sleep 10
      done

      if [ "$STATE" != "Succeeded" ]; then
        echo "ERROR: Certificate did not reach Succeeded state after 5 minutes (last state: $STATE)"
        exit 1
      fi

      # Step 4: Bind hostname with certificate
      az containerapp hostname bind \
        --hostname "${self.input.hostname}" \
        --name "${self.input.app_name}" \
        --resource-group "${self.input.rg_name}" \
        --environment "${self.input.env_name}"
    EOT
  }
}

# Static Web App: custom domain (CNAME delegation — Azure handles SSL automatically)
resource "azurerm_static_web_app_custom_domain" "frontend" {
  count             = var.frontend_custom_domain != "" ? 1 : 0
  static_web_app_id = module.static_web_app.static_web_app_id
  domain_name       = var.frontend_custom_domain
  validation_type   = "cname-delegation"
}
