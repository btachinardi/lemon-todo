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
  description = "Redis SKU (Basic, Standard, Premium)"
  type        = string
  default     = "Premium"
}

variable "family" {
  description = "Redis family (C for Basic/Standard, P for Premium)"
  type        = string
  default     = "P"
}

variable "capacity" {
  description = "Redis capacity (size of the cache)"
  type        = number
  default     = 1
}

variable "maxmemory_policy" {
  description = "Eviction policy"
  type        = string
  default     = "allkeys-lru"
}

variable "pe_subnet_id" {
  description = "Subnet ID for private endpoint (optional)"
  type        = string
  default     = ""
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}
