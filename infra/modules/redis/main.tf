resource "azurerm_redis_cache" "this" {
  name                = "redis-${var.project}-${var.environment}-${var.location_short}"
  location            = var.location
  resource_group_name = var.resource_group_name
  capacity            = var.capacity
  family              = var.family
  sku_name            = var.sku_name
  minimum_tls_version = "1.2"
  non_ssl_port_enabled = false

  redis_configuration {
    maxmemory_policy = var.maxmemory_policy
  }

  tags = var.tags
}

# Private endpoint for Redis (when subnet is provided)
resource "azurerm_private_endpoint" "redis" {
  count = var.pe_subnet_id != "" ? 1 : 0

  name                = "pe-redis-${var.project}-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  subnet_id           = var.pe_subnet_id

  private_service_connection {
    name                           = "redis-connection"
    private_connection_resource_id = azurerm_redis_cache.this.id
    subresource_names              = ["redisCache"]
    is_manual_connection           = false
  }

  tags = var.tags
}
