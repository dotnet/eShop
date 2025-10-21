variable "location" {
  description = "Azure region to deploy resources."
  type        = string
  default     = "westeurope"
}

variable "dns_zone_name" {
  description = "DNS zone name (e.g., contoso.com)."
  type        = string
  default     = "contoso.com"
}

variable "kubernetes_version" {
  description = "AKS Kubernetes version."
  type        = string
  default     = "1.29.0"
}

variable "apim_publisher_email" {
  description = "Email for APIM publisher."
  type        = string
  default     = "admin@contoso.com"
}
