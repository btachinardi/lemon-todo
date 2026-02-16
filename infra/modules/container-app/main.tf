# Container Registry for storing Docker images
resource "azurerm_container_registry" "this" {
  name                = "cr${var.project}${var.environment}${var.location_short}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "Basic"
  admin_enabled       = true

  tags = var.tags
}

# Container App Environment (managed Kubernetes under the hood)
resource "azurerm_container_app_environment" "this" {
  name                       = "cae-${var.project}-${var.environment}-${var.location_short}"
  location                   = var.location
  resource_group_name        = var.resource_group_name
  log_analytics_workspace_id = var.log_analytics_workspace_id

  tags = var.tags
}

# Container App (the actual running application)
resource "azurerm_container_app" "this" {
  name                         = "ca-${var.project}-${var.environment}-${var.location_short}"
  container_app_environment_id = azurerm_container_app_environment.this.id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  secret {
    name  = "registry-password"
    value = azurerm_container_registry.this.admin_password
  }

  secret {
    name  = "sql-connection-string"
    value = var.sql_connection_string
  }

  secret {
    name  = "jwt-secret-key"
    value = var.jwt_secret_key
  }

  secret {
    name  = "encryption-key"
    value = var.encryption_key
  }

  registry {
    server               = azurerm_container_registry.this.login_server
    username             = azurerm_container_registry.this.admin_username
    password_secret_name = "registry-password"
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 0
    max_replicas = var.max_replicas

    container {
      name   = "api"
      image  = "${azurerm_container_registry.this.login_server}/${var.project}-api:latest"
      cpu    = var.cpu
      memory = var.memory

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.aspnet_environment
      }

      env {
        name  = "DatabaseProvider"
        value = "SqlServer"
      }

      env {
        name        = "ConnectionStrings__DefaultConnection"
        secret_name = "sql-connection-string"
      }

      env {
        name        = "Jwt__SecretKey"
        secret_name = "jwt-secret-key"
      }

      env {
        name  = "Jwt__Issuer"
        value = "LemonDo"
      }

      env {
        name  = "Jwt__Audience"
        value = "LemonDo"
      }

      env {
        name        = "Encryption__FieldEncryptionKey"
        secret_name = "encryption-key"
      }

      env {
        name  = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        value = var.app_insights_connection_string
      }

      env {
        name  = "Cors__AllowedOrigins__0"
        value = var.cors_origin
      }

      dynamic "env" {
        for_each = var.cors_origin_secondary != "" ? [var.cors_origin_secondary] : []
        content {
          name  = "Cors__AllowedOrigins__1"
          value = env.value
        }
      }

      liveness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/alive"
      }

      readiness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/health"
      }

      startup_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/alive"
      }
    }
  }

  tags = var.tags
}
