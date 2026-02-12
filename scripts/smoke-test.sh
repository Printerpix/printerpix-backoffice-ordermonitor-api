#!/usr/bin/env bash
# Smoke test script for OrderMonitor API
# Usage: ./scripts/smoke-test.sh <base-url>
# Example: ./scripts/smoke-test.sh https://ordermonitor-api.staging.printerpix.com

set -euo pipefail

BASE_URL="${1:?Usage: $0 <base-url>}"
PASSED=0
FAILED=0
TOTAL=0

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

check_endpoint() {
  local name="$1"
  local url="$2"
  local expected_status="${3:-200}"
  local method="${4:-GET}"

  TOTAL=$((TOTAL + 1))
  printf "  %-40s " "${name}..."

  local status
  status=$(curl -s -o /dev/null -w "%{http_code}" -X "${method}" "${url}" --max-time 10 2>/dev/null || echo "000")

  if [ "${status}" = "${expected_status}" ]; then
    printf "${GREEN}PASS${NC} (HTTP %s)\n" "${status}"
    PASSED=$((PASSED + 1))
  else
    printf "${RED}FAIL${NC} (HTTP %s, expected %s)\n" "${status}" "${expected_status}"
    FAILED=$((FAILED + 1))
  fi
}

echo ""
echo "========================================="
echo " OrderMonitor API Smoke Tests"
echo " Target: ${BASE_URL}"
echo "========================================="
echo ""

# Health check
echo "--- Health ---"
check_endpoint "Health endpoint" "${BASE_URL}/health"

# Orders API
echo ""
echo "--- Orders API ---"
check_endpoint "Get stuck orders" "${BASE_URL}/api/orders/stuck"
check_endpoint "Get stuck orders summary" "${BASE_URL}/api/orders/stuck/summary"

# Alerts API
echo ""
echo "--- Alerts API ---"
check_endpoint "Test alert (POST)" "${BASE_URL}/api/alerts/test" "200" "POST"

# Diagnostics API
echo ""
echo "--- Diagnostics API ---"
check_endpoint "List tables" "${BASE_URL}/api/diagnostics/tables"

# Swagger
echo ""
echo "--- Swagger ---"
check_endpoint "Swagger JSON" "${BASE_URL}/swagger/v1/swagger.json"

# Summary
echo ""
echo "========================================="
printf " Results: ${GREEN}%d passed${NC}, ${RED}%d failed${NC}, %d total\n" "${PASSED}" "${FAILED}" "${TOTAL}"
echo "========================================="
echo ""

if [ "${FAILED}" -gt 0 ]; then
  echo -e "${RED}Smoke tests FAILED${NC}"
  exit 1
else
  echo -e "${GREEN}All smoke tests PASSED${NC}"
  exit 0
fi
