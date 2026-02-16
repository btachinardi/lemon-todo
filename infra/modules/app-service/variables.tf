variable "project" {
  description = "Project name for resource naming"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "location_short" {
  description = "Short region code for naming"
  type        = string
}

variable "resource_group_name" {
  description = "Resource group to deploy into"
  type        = string
}

variable "sku_name" {
  description = "App Service Plan SKU (e.g., B1, S1, P2v3)"
  type        = string
  default     = "B1"
}

variable "always_on" {
  description = "Keep app always running (requires Basic+ tier)"
  type        = bool
  default     = true
}

variable "aspnet_environment" {
  description = "ASP.NET Core environment name"
  type        = string
  default     = "Production"
}

variable "sql_connection_string" {
  description = "SQL Server connection string"
  type        = string
  sensitive   = true
}

variable "app_insights_connection_string" {
  description = "Application Insights connection string"
  type        = string
  default     = ""
  sensitive   = true
}

variable "key_vault_id" {
  description = "Key Vault resource ID for managed identity access"
  type        = string
  default     = ""
}

variable "enable_key_vault_access" {
  description = "Whether to grant managed identity access to Key Vault"
  type        = bool
  default     = false
}

variable "enable_staging_slot" {
  description = "Enable a staging deployment slot"
  type        = bool
  default     = false
}

variable "extra_app_settings" {
  description = "Additional app settings to merge"
  type        = map(string)
  default     = {}
}

variable "subnet_id" {
  description = "Subnet ID for VNet integration (optional)"
  type        = string
  default     = ""
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}
