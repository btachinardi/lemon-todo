variable "project" {
  description = "Project name for resource naming"
  type        = string
}

variable "environment" {
  description = "Environment name"
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
  description = "Front Door SKU (Standard_AzureFrontDoor or Premium_AzureFrontDoor)"
  type        = string
  default     = "Standard_AzureFrontDoor"
}

variable "app_service_hostname" {
  description = "App Service default hostname (origin)"
  type        = string
}

variable "enable_waf" {
  description = "Enable WAF policy"
  type        = bool
  default     = true
}

variable "waf_mode" {
  description = "WAF mode (Prevention or Detection)"
  type        = string
  default     = "Prevention"
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}
