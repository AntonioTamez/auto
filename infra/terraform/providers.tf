terraform {
  required_version = ">= 1.9.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # Backend parcial a propósito: storage account, container y key reales del
  # state remoto se pasan vía `-backend-config` en `terraform init` (ver
  # README.md). Nunca se hardcodean ni se versionan aquí.
  backend "azurerm" {}
}

provider "azurerm" {
  features {}
}
