output "resource_group_name" {
  description = "Resource group containing the state backend"
  value       = azurerm_resource_group.tfstate.name
}

output "storage_account_name" {
  description = "Storage account for Terraform state"
  value       = azurerm_storage_account.tfstate.name
}

output "container_name" {
  description = "Blob container for Terraform state"
  value       = azurerm_storage_container.tfstate.name
}

output "backend_config" {
  description = "Backend configuration block for stage modules"
  value       = <<-EOT
    backend "azurerm" {
      resource_group_name  = "${azurerm_resource_group.tfstate.name}"
      storage_account_name = "${azurerm_storage_account.tfstate.name}"
      container_name       = "${azurerm_storage_container.tfstate.name}"
      key                  = "<stage-name>.tfstate"
    }
  EOT
}
