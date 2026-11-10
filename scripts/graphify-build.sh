#!/usr/bin/env bash
# Build the local graphify graph (graphify-out/) from code plus the shared
# semantic cache committed in graphify-out/cache/semantic/.
#
# Usage: scripts/graphify-build.sh [--extract]
#   (default)  No LLM calls. Replays the shared semantic cache for docs/images.
#              If some docs changed since the last shared extraction, falls back
#              to an AST-only update that keeps any semantic nodes already built.
#   --extract  Also LLM-extract changed docs/images with the local `claude` CLI.
#              Afterwards commit the new files in graphify-out/cache/semantic/.
set -u
cd "$(git rev-parse --show-toplevel)" || exit 1
command -v graphify >/dev/null 2>&1 || { echo "graphify not installed (see scripts/setup.sh)" >&2; exit 1; }

# The shared cache is only reused by the graphify version that wrote it.
GRAPHIFY_VERSION="0.9.105"   # keep in sync with scripts/setup.sh
installed="$(graphify --version 2>/dev/null | awk '{print $NF}')"
if [ "$installed" != "$GRAPHIFY_VERSION" ]; then
  echo "[graphify] WARNING: graphify $installed installed, repo pins $GRAPHIFY_VERSION; the shared semantic cache won't be reused." >&2
  echo "[graphify] fix: uv tool install --force 'graphifyy[sql]==$GRAPHIFY_VERSION'  (or re-run scripts/setup.sh)" >&2
  if [ "${1:-}" = "--extract" ]; then
    echo "[graphify] refusing --extract with a mismatched version (it would write an unshared cache folder)." >&2
    exit 1
  fi
fi

log="$(mktemp)"
trap 'rm -f "$log"' EXIT

if [ "${1:-}" = "--extract" ]; then
  if ! command -v claude >/dev/null 2>&1; then
    echo "[graphify] --extract needs the 'claude' CLI, which is not installed." >&2
    echo "[graphify] run 'scripts/graphify-build.sh' (no LLM) instead; someone with the CLI can run --extract later." >&2
    exit 2
  fi
  graphify extract . --backend claude-cli && graphify cluster-only . --no-label
  exit $?
fi

# The dummy key only satisfies extract's up-front key check; every cached file is
# replayed locally and nothing is sent to an API.
if ANTHROPIC_API_KEY=graphify-cache-only graphify extract . --backend claude >"$log" 2>&1 \
  && graphify cluster-only . --no-label >>"$log" 2>&1; then
  echo "[graphify] graph built from code + shared semantic cache"
elif graphify update . >"$log" 2>&1; then
  echo "[graphify] some docs/images are not in the shared semantic cache; built code graph only for them."
  echo "[graphify] run 'scripts/graphify-build.sh --extract' and commit graphify-out/cache/semantic/ to share them."
else
  cat "$log" >&2
  exit 1
fi
