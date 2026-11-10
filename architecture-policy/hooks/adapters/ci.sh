#!/usr/bin/env bash
# CI/CD adapter (Linux runners). Thin wrapper only - all rules and decisions live in the policy pack.
# Usage: ci.sh [extra arch.py validate args]   e.g. ci.sh --base origin/main --test-results 'artifacts/**/*.trx'
set -euo pipefail
REPO_ROOT="$(git rev-parse --show-toplevel)"
OUT_DIR="${ARCH_OUT_DIR:-$REPO_ROOT/artifacts/architecture}"
mkdir -p "$OUT_DIR"
PYTHON="${PYTHON:-python3}"
"$PYTHON" -m pip install --quiet -r "$REPO_ROOT/architecture-policy/requirements.txt"
"$PYTHON" "$REPO_ROOT/architecture-policy/hooks/arch.py" selfcheck --skip-self-tests
set +e
"$PYTHON" "$REPO_ROOT/architecture-policy/hooks/arch.py" validate --checkpoint ci --caller ci \
  --format sarif --output "$OUT_DIR/architecture.sarif" "$@" > /dev/null
code=$?
"$PYTHON" "$REPO_ROOT/architecture-policy/hooks/arch.py" validate --checkpoint ci --caller ci --no-evidence --no-history "$@"
exit $code
