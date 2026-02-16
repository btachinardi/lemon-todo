resource "azurerm_storage_account" "static" {
  name                     = "st${var.project}cdn${var.environment}"
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = var.storage_replication_type
  min_tls_version          = "TLS1_2"

  blob_properties {
    cors_rule {
      allowed_headers    = ["*"]
      allowed_methods    = ["GET", "HEAD"]
      allowed_origins    = var.allowed_origins
      exposed_headers    = ["*"]
      max_age_in_seconds = 3600
    }
  }

  tags = var.tags
}

resource "azurerm_storage_account_static_website" "static" {
  storage_account_id = azurerm_storage_account.static.id
  index_document     = "index.html"
  error_404_document = "index.html"
}

resource "azurerm_cdn_frontdoor_origin_group" "static" {
  name                     = "static-origin-group"
  cdn_frontdoor_profile_id = var.frontdoor_profile_id

  load_balancing {
    sample_size                 = 4
    successful_samples_required = 3
  }
}

resource "azurerm_cdn_frontdoor_origin" "static" {
  name                          = "static-origin"
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.static.id
  enabled                       = true

  host_name          = azurerm_storage_account.static.primary_web_host
  http_port          = 80
  https_port         = 443
  origin_host_header = azurerm_storage_account.static.primary_web_host
  certificate_name_check_enabled = true
}
