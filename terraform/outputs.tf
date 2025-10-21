output "resource_group_name" {
  value = azurerm_resource_group.eshop.name
}

output "aks_cluster_name" {
  value = module.aks.aks_name
}

output "frontdoor_endpoint" {
  value = azurerm_cdn_frontdoor_profile.eshop.endpoint
}

output "apim_name" {
  value = azurerm_api_management.eshop.name
}

output "private_link_service_name" {
  value = azurerm_private_link_service.eshop.name
}
