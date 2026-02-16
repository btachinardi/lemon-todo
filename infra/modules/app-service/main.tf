resource "azurerm_service_plan" "this" {
  name                = "plan-${var.project}-${var.environment}-${var.location_short}"
  location            = var.location
  resource_group_name = var.resource_group_name
  os_type             = "Linux"
  sku_name            = var.sku_name

  tags = var.tags
}

resource "azurerm_linux_web_app" "this" {
  name                = "app-${var.project}-${var.environment}-${var.location_short}"
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = azurerm_service_plan.this.id
  https_only          = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on                         = var.always_on
    health_check_path                 = "/alive"
    health_check_eviction_time_in_min = 5

    application_stack {
      dotnet_version = "10.0"
    }
  }

  app_settings = merge(
    {
      "ASPNETCORE_ENVIRONMENT"                  = var.aspnet_environment
      "DatabaseProvider"                         = "SqlServer"
      "APPLICATIONINSIGHTS_CONNECTION_STRING"    = var.app_insights_connection_string
      "ApplicationInsightsAgent_EXTENSION_VERSION" = "~3"
    },
    var.extra_app_settings
  )

  connection_string {
    name  = "DefaultConnection"
    type  = "SQLAzure"
    value = var.sql_connection_string
  }

  logs {
    http_logs {
      file_system {
        retention_in_days = 7
        retention_in_mb   = 35
      }
    }
  }

  tags = var.tags
}

# Grant managed identity access to Key Vault secrets
resource "azurerm_role_assignment" "keyvault_reader" {
  count = var.enable_key_vault_access ? 1 : 0

  scope                = var.key_vault_id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_linux_web_app.this.identity[0].principal_id
}

# Staging slot (when enabled)
resource "azurerm_linux_web_app_slot" "staging" {
  count = var.enable_staging_slot ? 1 : 0

  name           = "staging"
  app_service_id = azurerm_linux_web_app.this.id
  https_only     = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on                         = false
    health_check_path                 = "/alive"
    health_check_eviction_time_in_min = 5

    application_stack {
      dotnet_version = "10.0"
    }
  }

  app_settings = azurerm_linux_web_app.this.app_settings

  connection_string {
    name  = "DefaultConnection"
    type  = "SQLAzure"
    value = var.sql_connection_string
  }

  tags = var.tags
}
