#!/usr/bin/env bash
set -euo pipefail

# Bootstrap the Terraform state backend in Azure.
# Run this ONCE before deploying any stage.
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - Terraform installed
#
# Usage: ./bootstrap.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BOOTSTRAP_DIR="$SCRIPT_DIR/../bootstrap"

echo "=== LemonDo Terraform Bootstrap ==="
echo ""

if [ ! -f "$BOOTSTRAP_DIR/terraform.tfvars" ]; then
  echo "ERROR: $BOOTSTRAP_DIR/terraform.tfvars not found."
  echo "Copy terraform.tfvars.example and fill in your values:"
  echo "  cp $BOOTSTRAP_DIR/terraform.tfvars.example $BOOTSTRAP_DIR/terraform.tfvars"
  exit 1
fi

cd "$BOOTSTRAP_DIR"

echo "Initializing Terraform..."
terraform init

echo ""
echo "Planning bootstrap resources..."
terraform plan -out=bootstrap.tfplan

echo ""
read -p "Apply bootstrap plan? (y/N) " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
  terraform apply bootstrap.tfplan
  echo ""
  echo "Bootstrap complete! State backend configuration:"
  terraform output backend_config
else
  echo "Aborted."
  rm -f bootstrap.tfplan
fi
