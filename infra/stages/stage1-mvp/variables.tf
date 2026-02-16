variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "eastus2"
}

variable "location_short" {
  description = "Short region code for naming"
  type        = string
  default     = "eus2"
}

variable "sql_admin_login" {
  description = "SQL Server administrator login"
  type        = string
  sensitive   = true
}

variable "sql_admin_password" {
  description = "SQL Server administrator password"
  type        = string
  sensitive   = true
}

variable "aad_admin_login" {
  description = "Azure AD administrator login"
  type        = string
}

variable "aad_admin_object_id" {
  description = "Azure AD administrator object ID"
  type        = string
}

variable "jwt_secret_key" {
  description = "JWT signing secret key"
  type        = string
  sensitive   = true
}

variable "encryption_key" {
  description = "Field encryption key (base64)"
  type        = string
  sensitive   = true
}

variable "api_custom_domain" {
  description = "Custom domain for the API Container App (e.g. api.lemondo.btas.dev). Leave empty to skip."
  type        = string
  default     = ""
}

variable "frontend_custom_domain" {
  description = "Custom domain for the frontend Static Web App (e.g. lemondo.btas.dev). Leave empty to skip."
  type        = string
  default     = ""
}
