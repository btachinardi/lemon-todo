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

variable "resource_group_name" {
  description = "Resource group to deploy into"
  type        = string
}

variable "frontdoor_profile_id" {
  description = "Front Door profile ID for CDN origin group"
  type        = string
}

variable "storage_replication_type" {
  description = "Storage replication type (LRS, GRS, ZRS)"
  type        = string
  default     = "LRS"
}

variable "allowed_origins" {
  description = "CORS allowed origins for static storage"
  type        = list(string)
  default     = ["*"]
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}
