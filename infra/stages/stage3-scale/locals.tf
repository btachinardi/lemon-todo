locals {
  project     = "lemondo"
  environment = "scale"

  tags = {
    project     = local.project
    environment = local.environment
    managed_by  = "terraform"
    stage       = "3-scale"
  }
}
