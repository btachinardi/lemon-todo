locals {
  project     = "lemondo"
  environment = "prod"

  tags = {
    project     = local.project
    environment = local.environment
    managed_by  = "terraform"
    stage       = "2-resilience"
  }
}
