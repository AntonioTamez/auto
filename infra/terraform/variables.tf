variable "environment" {
  description = <<-EOT
    Ambiente de despliegue (dev, staging o prod). Sin default a propósito:
    debe pasarse explícitamente en cada `plan`/`apply` (-var environment=dev)
    para evitar que una corrida sin flags aterrice silenciosamente en el
    ambiente equivocado.
  EOT
  type        = string

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "environment debe ser uno de: dev, staging, prod."
  }
}

variable "location" {
  description = "Región de Azure donde se crean todos los recursos. Fija en mexicocentral (costo equivalente a South Central US, mejor latencia/residencia para Monterrey)."
  type        = string
  default     = "mexicocentral"

  validation {
    condition     = can(regex("^[a-z0-9]+$", var.location))
    error_message = "location debe ser un nombre de región de Azure válido (minúsculas/números, sin espacios ni guiones, ej. mexicocentral)."
  }
}

variable "project_prefix" {
  description = "Prefijo de nombre de proyecto usado en todos los recursos. Propuesto 'auto' mientras el nombre de producto es un placeholder en el PRD."
  type        = string
  default     = "auto"

  validation {
    condition     = can(regex("^[a-z0-9]{1,8}$", var.project_prefix))
    error_message = "project_prefix debe ser minúsculas/números, 1-8 caracteres (se interpola en storage_account_name, que exige 3-24 caracteres alfanuméricos en minúscula)."
  }
}
