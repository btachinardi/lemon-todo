terraform {
  required_version = ">= 1.5"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  backend "azurerm" {
    resource_group_name  = "rg-lemondo-tfstate-eus2"
    storage_account_name = "stlemondotfstateeus2"
    container_name       = "tfstate"
    key                  = "stage1-mvp.tfstate"
  }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}
