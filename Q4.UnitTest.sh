#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
Q4_DIR="$ROOT_DIR/TorchSharp.Q4.Extension"
PY_TEST_DIR="$ROOT_DIR/dev/test"

if ! command -v dotnet >/dev/null 2>&1; then
  export PATH="$PATH:/usr/local/bin/dotnet-sdk"
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "[Q4.UnitTest] dotnet not found. Tried PATH+=/usr/local/bin/dotnet-sdk"
  exit 127
fi

MODE="${1:-core}"

run_core_tests() {
  echo "[Q4.UnitTest] Running core tests (build + TestCase.fsx)"
  dotnet build -c Release "$Q4_DIR/TorchSharp.Q4.Extension.fsproj"
  (
    cd "$Q4_DIR"
    dotnet fsi TestCase.fsx
  )
}

run_python_tests() {
  echo "[Q4.UnitTest] Running python tests under $PY_TEST_DIR"

  if ! command -v python3 >/dev/null 2>&1; then
    echo "[Q4.UnitTest] python3 not found."
    exit 127
  fi

  if [ ! -d "$PY_TEST_DIR" ]; then
    echo "[Q4.UnitTest] test directory not found: $PY_TEST_DIR"
    exit 2
  fi

  mapfile -t tests < <(find "$PY_TEST_DIR" -maxdepth 1 -type f -name "test_*.py" | sort)
  if [ "${#tests[@]}" -eq 0 ]; then
    echo "[Q4.UnitTest] no python tests found."
    return 0
  fi

  local failed=0
  for t in "${tests[@]}"; do
    echo "[Q4.UnitTest] python3 $t"
    if ! python3 "$t"; then
      echo "[Q4.UnitTest] FAILED: $t"
      failed=1
    fi
  done

  if [ "$failed" -ne 0 ]; then
    echo "[Q4.UnitTest] python test failures detected."
    exit 1
  fi
}

case "$MODE" in
  core)
    run_core_tests
    ;;
  all)
    run_core_tests
    run_python_tests
    ;;
  py|python)
    run_python_tests
    ;;
  *)
    echo "Usage: $0 [core|all|py]"
    exit 2
    ;;
esac

echo "[Q4.UnitTest] Completed successfully."
