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

variable "admin_login" {
  description = "SQL Server administrator login"
  type        = string
  sensitive   = true
}

variable "admin_password" {
  description = "SQL Server administrator password"
  type        = string
  sensitive   = true
}

variable "aad_admin_login" {
  description = "Azure AD administrator login name"
  type        = string
}

variable "aad_admin_object_id" {
  description = "Azure AD administrator object ID"
  type        = string
}

variable "sku_name" {
  description = "Database SKU (e.g., Basic, S0, S1, P1)"
  type        = string
  default     = "Basic"
}

variable "max_size_gb" {
  description = "Maximum database size in GB"
  type        = number
  default     = 2
}

variable "zone_redundant" {
  description = "Enable zone redundancy"
  type        = bool
  default     = false
}

variable "geo_backup_enabled" {
  description = "Enable geo-redundant backups"
  type        = bool
  default     = false
}

variable "read_scale" {
  description = "Enable read scale-out"
  type        = bool
  default     = false
}

variable "backup_storage_redundancy" {
  description = "Backup storage redundancy (Local, Zone, Geo, GeoZone)"
  type        = string
  default     = "Local"
}

variable "short_term_retention_days" {
  description = "Short-term backup retention in days"
  type        = number
  default     = 7
}

variable "allow_azure_services" {
  description = "Allow Azure services to access the SQL server"
  type        = bool
  default     = true
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}
