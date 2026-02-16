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

variable "log_analytics_workspace_id" {
  description = "Log Analytics Workspace ID for the Container App Environment"
  type        = string
}

variable "sql_connection_string" {
  description = "SQL Server connection string"
  type        = string
  sensitive   = true
}

variable "jwt_secret_key" {
  description = "JWT signing secret"
  type        = string
  sensitive   = true
}

variable "encryption_key" {
  description = "AES field encryption key"
  type        = string
  sensitive   = true
}

variable "app_insights_connection_string" {
  description = "Application Insights connection string"
  type        = string
  default     = ""
  sensitive   = true
}

variable "cors_origin" {
  description = "Allowed CORS origin (frontend URL)"
  type        = string
  default     = "*"
}

variable "aspnet_environment" {
  description = "ASP.NET Core environment name"
  type        = string
  default     = "Production"
}

variable "cpu" {
  description = "CPU cores for the container (e.g., 0.25, 0.5, 1.0)"
  type        = number
  default     = 0.25
}

variable "memory" {
  description = "Memory for the container (e.g., 0.5Gi, 1Gi)"
  type        = string
  default     = "0.5Gi"
}

variable "max_replicas" {
  description = "Maximum number of replicas for auto-scaling"
  type        = number
  default     = 3
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}
