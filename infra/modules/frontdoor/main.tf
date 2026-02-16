resource "azurerm_cdn_frontdoor_profile" "this" {
  name                = "afd-${var.project}-${var.environment}-${var.location_short}"
  resource_group_name = var.resource_group_name
  sku_name            = var.sku_name

  tags = var.tags
}

resource "azurerm_cdn_frontdoor_endpoint" "api" {
  name                     = "api-${var.project}-${var.environment}"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.this.id

  tags = var.tags
}

resource "azurerm_cdn_frontdoor_origin_group" "api" {
  name                     = "api-origin-group"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.this.id

  health_probe {
    path                = "/alive"
    protocol            = "Https"
    interval_in_seconds = 30
  }

  load_balancing {
    sample_size                 = 4
    successful_samples_required = 3
  }
}

resource "azurerm_cdn_frontdoor_origin" "api" {
  name                          = "api-origin"
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.api.id
  enabled                       = true

  host_name          = var.app_service_hostname
  http_port          = 80
  https_port         = 443
  origin_host_header = var.app_service_hostname
  certificate_name_check_enabled = true
}

resource "azurerm_cdn_frontdoor_route" "api" {
  name                          = "api-route"
  cdn_frontdoor_endpoint_id     = azurerm_cdn_frontdoor_endpoint.api.id
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.api.id
  cdn_frontdoor_origin_ids      = [azurerm_cdn_frontdoor_origin.api.id]

  supported_protocols    = ["Https"]
  https_redirect_enabled = true
  patterns_to_match      = ["/*"]

  cdn_frontdoor_origin_path = "/"

  link_to_default_domain = true
}

# WAF policy
resource "azurerm_cdn_frontdoor_firewall_policy" "this" {
  count = var.enable_waf ? 1 : 0

  name                = "waf${var.project}${var.environment}"
  resource_group_name = var.resource_group_name
  sku_name            = var.sku_name
  mode                = var.waf_mode

  managed_rule {
    type    = "Microsoft_DefaultRuleSet"
    version = "2.1"
    action  = "Block"
  }

  managed_rule {
    type    = "Microsoft_BotManagerRuleSet"
    version = "1.1"
    action  = "Block"
  }

  tags = var.tags
}

resource "azurerm_cdn_frontdoor_security_policy" "this" {
  count = var.enable_waf ? 1 : 0

  name                     = "waf-policy"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.this.id

  security_policies {
    firewall {
      cdn_frontdoor_firewall_policy_id = azurerm_cdn_frontdoor_firewall_policy.this[0].id

      association {
        domain {
          cdn_frontdoor_domain_id = azurerm_cdn_frontdoor_endpoint.api.id
        }
        patterns_to_match = ["/*"]
      }
    }
  }
}
