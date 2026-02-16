#!/usr/bin/env bash
set -euo pipefail

# Deploy a specific stage of LemonDo infrastructure.
#
# Prerequisites:
#   - Azure CLI installed and logged in
#   - Terraform installed
#   - Bootstrap already completed (./bootstrap.sh)
#
# Usage: ./deploy.sh <stage>
#   stage: stage1-mvp | stage2-resilience | stage3-scale

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STAGES_DIR="$SCRIPT_DIR/../stages"

if [ $# -lt 1 ]; then
  echo "Usage: $0 <stage>"
  echo "  Stages: stage1-mvp, stage2-resilience, stage3-scale"
  exit 1
fi

STAGE="$1"
STAGE_DIR="$STAGES_DIR/$STAGE"

if [ ! -d "$STAGE_DIR" ]; then
  echo "ERROR: Stage directory not found: $STAGE_DIR"
  echo "Available stages:"
  ls -1 "$STAGES_DIR"
  exit 1
fi

if [ ! -f "$STAGE_DIR/terraform.tfvars" ]; then
  echo "ERROR: $STAGE_DIR/terraform.tfvars not found."
  echo "Copy terraform.tfvars.example and fill in your values:"
  echo "  cp $STAGE_DIR/terraform.tfvars.example $STAGE_DIR/terraform.tfvars"
  exit 1
fi

echo "=== Deploying LemonDo: $STAGE ==="
echo ""

cd "$STAGE_DIR"

echo "Initializing Terraform..."
terraform init -upgrade

echo ""
echo "Validating configuration..."
terraform validate

echo ""
echo "Planning deployment..."
terraform plan -out=deploy.tfplan

echo ""
read -p "Apply deployment plan? (y/N) " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
  terraform apply deploy.tfplan
  echo ""
  echo "=== Deployment complete ==="
  echo ""
  terraform output
else
  echo "Aborted."
  rm -f deploy.tfplan
fi
