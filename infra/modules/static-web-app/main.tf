resource "azurerm_static_web_app" "this" {
  name                = "swa-${var.project}-${var.environment}-${var.location_short}"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku_tier            = var.sku_tier
  sku_size            = var.sku_tier

  tags = var.tags
}
