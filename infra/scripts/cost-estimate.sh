#!/usr/bin/env bash
set -euo pipefail

# Generate cost estimates for LemonDo infrastructure stages using infracost.
#
# Prerequisites:
#   - infracost installed (https://www.infracost.io/docs/)
#   - INFRACOST_API_KEY set
#
# Usage: ./cost-estimate.sh [stage]
#   stage: stage1-mvp | stage2-resilience | stage3-scale | all (default)

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STAGES_DIR="$SCRIPT_DIR/../stages"

STAGE="${1:-all}"

estimate_stage() {
  local stage_name="$1"
  local stage_dir="$STAGES_DIR/$stage_name"

  if [ ! -d "$stage_dir" ]; then
    echo "WARNING: Stage directory not found: $stage_dir"
    return
  fi

  echo ""
  echo "=== Cost Estimate: $stage_name ==="
  echo ""

  infracost breakdown \
    --path "$stage_dir" \
    --terraform-var-file "$stage_dir/terraform.tfvars.example" \
    2>/dev/null || echo "  (Run 'infracost configure' if API key is not set)"
}

if [ "$STAGE" = "all" ]; then
  for stage_dir in "$STAGES_DIR"/stage*; do
    stage_name="$(basename "$stage_dir")"
    estimate_stage "$stage_name"
  done

  echo ""
  echo "=== Summary ==="
  echo "Run with a specific stage for detailed breakdown:"
  echo "  $0 stage1-mvp"
else
  estimate_stage "$STAGE"
fi
