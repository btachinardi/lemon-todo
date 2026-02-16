variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
}

variable "location" {
  description = "Azure region for the state backend"
  type        = string
  default     = "eastus2"
}

variable "location_short" {
  description = "Short region code for naming (e.g., eus2)"
  type        = string
  default     = "eus2"
}
