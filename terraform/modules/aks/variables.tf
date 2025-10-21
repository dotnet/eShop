variable "resource_group_name" {
  type = string
}
variable "kubernetes_version" {
  type = string
}
variable "prefix" {
  type = string
}
variable "vnet_subnet_id" {
  type = string
}
variable "network_plugin" {
  type = string
}
variable "node_pools" {
  type = any
}
