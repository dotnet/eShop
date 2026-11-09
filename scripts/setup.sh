#!/usr/bin/env bash
# One-shot setup for humans and AI agents (macOS/Linux): installs prerequisites,
# configures graphify + git hooks, builds the solution and starts the app.
#
# Usage: scripts/setup.sh [--no-run] [--skip-build]
#   --no-run      set everything up but do not start the app
#   --skip-build  skip `dotnet build`
set -euo pipefail

RUN_APP=1; BUILD=1
for a in "$@"; do
  case "$a" in
    --no-run) RUN_APP=0 ;;
    --skip-build) BUILD=0 ;;
    *) echo "Unknown option: $a" >&2; exit 2 ;;
  esac
done

cd "$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.aspire/bin:$HOME/.local/bin:$PATH"

step() { printf '\n==> %s\n' "$*"; }
have() { command -v "$1" >/dev/null 2>&1; }

# --- .NET SDK (version pinned in global.json) ---
step ".NET SDK"
want_sdk="$(sed -n 's/.*"version": *"\([0-9][^"]*\)".*/\1/p' global.json | head -1)"
if ! dotnet --version >/dev/null 2>&1; then
  echo "Installing .NET SDK ${want_sdk}"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --version "$want_sdk" --install-dir "$HOME/.dotnet"
  export DOTNET_ROOT="$HOME/.dotnet"
fi
dotnet --version

# --- Aspire CLI ---
step "Aspire CLI"
if ! have aspire; then
  curl -fsSL https://aspire.dev/install.sh | bash 2>&1 | tr "\r" "\n" | grep -v "%" || true
fi
aspire --version

# --- Container runtime (required by the AppHost) ---
step "Container runtime"
if have docker && docker info >/dev/null 2>&1; then
  echo "Docker is running"
elif have podman; then
  echo "Podman found (see https://aspire.dev/get-started/prerequisites/ if not configured)"
else
  echo "No running container runtime. Install/start Docker Desktop (or Podman) before running the app." >&2
  [ "$RUN_APP" = 1 ] && exit 1
fi

# --- Node deps (Playwright e2e) ---
step "Node dependencies"
if have npm; then npm ci; else echo "npm not found; skipping (only needed for e2e tests)"; fi

# --- graphify ---
step "graphify"
if ! have graphify; then
  if have pipx; then pipx install graphifyy
  else python3 -m pip install --user --break-system-packages graphifyy 2>/dev/null \
    || python3 -m pip install --user graphifyy; fi
fi
have graphify || { echo "ERROR: graphify not on PATH after install" >&2; exit 1; }
# Agent instructions/hooks are committed; these are idempotent refreshes.
graphify claude install >/dev/null
graphify codex install >/dev/null
git config core.hooksPath .githooks
chmod +x .githooks/*
graphify update .
echo "graphify ready; local graph auto-refreshes after commit/merge/checkout (core.hooksPath=.githooks)"

# --- Build ---
if [ "$BUILD" = 1 ]; then
  step "dotnet build"
  dotnet build eShop.Web.slnf
fi

# --- Run ---
if [ "$RUN_APP" = 1 ]; then
  step "Starting app (aspire start)"
  aspire start
  aspire wait webapp || true
  aspire ps
  echo "Stop with: aspire stop"
fi

step "Done"
