variable "name" {
  type = string
}
variable "location" {
  type = string
}
variable "resource_group_name" {
  type = string
}
variable "subnet_id" {
  type = string
}
variable "load_balancer_frontend_ip_configuration_ids" {
  type = list(string)
  default = []
}
