#!/usr/bin/env sh
# Git pre-commit adapter. Install: git config core.hooksPath .githooks  (with .githooks/pre-commit calling this file)
# or: ln -s ../../architecture-policy/hooks/adapters/pre-commit.sh .git/hooks/pre-commit
# Thin wrapper only - all rules and decisions live in the policy pack.
set -e
REPO_ROOT="$(git rev-parse --show-toplevel)"
PYTHON="${PYTHON:-python3}"
command -v "$PYTHON" >/dev/null 2>&1 || PYTHON=python
exec "$PYTHON" "$REPO_ROOT/architecture-policy/hooks/arch.py" validate \
  --checkpoint pre_commit --caller git-pre-commit --no-evidence "$@"
