locals {
  project     = "lemondo"
  environment = "mvp"

  tags = {
    project     = local.project
    environment = local.environment
    managed_by  = "terraform"
    stage       = "1-mvp"
  }
}
