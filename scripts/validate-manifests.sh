#!/usr/bin/env bash
# Validate Kubernetes manifests
# Usage: ./scripts/validate-manifests.sh

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "${SCRIPT_DIR}")"
K8S_DIR="${ROOT_DIR}/k8s"

PASSED=0
FAILED=0

validate_kustomize() {
  local overlay="$1"
  local path="${K8S_DIR}/overlays/${overlay}"

  printf "  Validating %-20s " "${overlay}..."

  if command -v kustomize &>/dev/null; then
    if kustomize build "${path}" >/dev/null 2>&1; then
      printf "${GREEN}PASS${NC}\n"
      PASSED=$((PASSED + 1))
    else
      printf "${RED}FAIL${NC}\n"
      FAILED=$((FAILED + 1))
      kustomize build "${path}" 2>&1 | head -5
    fi
  elif command -v kubectl &>/dev/null; then
    if kubectl kustomize "${path}" >/dev/null 2>&1; then
      printf "${GREEN}PASS${NC}\n"
      PASSED=$((PASSED + 1))
    else
      printf "${RED}FAIL${NC}\n"
      FAILED=$((FAILED + 1))
      kubectl kustomize "${path}" 2>&1 | head -5
    fi
  else
    printf "${RED}SKIP${NC} (no kustomize or kubectl found)\n"
  fi
}

validate_yaml() {
  local file="$1"
  local name
  name=$(basename "${file}")

  printf "  Validating %-30s " "${name}..."

  if command -v kubectl &>/dev/null; then
    if kubectl apply --dry-run=client -f "${file}" >/dev/null 2>&1; then
      printf "${GREEN}PASS${NC}\n"
      PASSED=$((PASSED + 1))
    else
      printf "${RED}FAIL${NC}\n"
      FAILED=$((FAILED + 1))
    fi
  else
    # Basic YAML syntax check with python
    if python3 -c "import yaml; yaml.safe_load(open('${file}'))" 2>/dev/null; then
      printf "${GREEN}PASS${NC} (yaml syntax only)\n"
      PASSED=$((PASSED + 1))
    else
      printf "${RED}FAIL${NC}\n"
      FAILED=$((FAILED + 1))
    fi
  fi
}

echo ""
echo "========================================="
echo " K8s Manifest Validation"
echo "========================================="
echo ""

echo "--- Base Manifests ---"
for f in "${K8S_DIR}/base/"*.yaml; do
  [ -f "$f" ] && [ "$(basename "$f")" != "kustomization.yaml" ] && validate_yaml "$f"
done

echo ""
echo "--- Kustomize Overlays ---"
validate_kustomize "staging"
validate_kustomize "production"

echo ""
echo "========================================="
printf " Results: ${GREEN}%d passed${NC}, ${RED}%d failed${NC}\n" "${PASSED}" "${FAILED}"
echo "========================================="

if [ "${FAILED}" -gt 0 ]; then
  exit 1
fi
