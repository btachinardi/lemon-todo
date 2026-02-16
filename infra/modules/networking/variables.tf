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

variable "vnet_address_space" {
  description = "VNet address space CIDR"
  type        = string
  default     = "10.0.0.0/16"
}

variable "app_subnet_prefix" {
  description = "App Service subnet CIDR"
  type        = string
  default     = "10.0.1.0/24"
}

variable "pe_subnet_prefix" {
  description = "Private endpoints subnet CIDR"
  type        = string
  default     = "10.0.2.0/24"
}

variable "enable_sql_private_endpoint" {
  description = "Create private endpoint for SQL Server"
  type        = bool
  default     = false
}

variable "sql_server_id" {
  description = "SQL Server resource ID for private endpoint"
  type        = string
  default     = ""
}

variable "app_service_id" {
  description = "App Service resource ID for VNet integration"
  type        = string
  default     = ""
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}
