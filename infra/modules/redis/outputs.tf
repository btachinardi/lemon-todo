output "redis_id" {
  description = "Redis Cache resource ID"
  value       = azurerm_redis_cache.this.id
}

output "hostname" {
  description = "Redis Cache hostname"
  value       = azurerm_redis_cache.this.hostname
}

output "ssl_port" {
  description = "Redis Cache SSL port"
  value       = azurerm_redis_cache.this.ssl_port
}

output "connection_string" {
  description = "Redis Cache primary connection string"
  value       = azurerm_redis_cache.this.primary_connection_string
  sensitive   = true
}
