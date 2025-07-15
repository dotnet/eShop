#!/usr/bin/env bash

set -e -o pipefail

CURRENT_ARCH=$(uname -m)
case "$CURRENT_ARCH" in
  arm64|aarch64)
    DOCKER_PLATFORM="linux/arm64"
    ;;
  x86_64|amd64)
    DOCKER_PLATFORM="linux/amd64"
    ;;
  *)
    echo "Unsupported architecture: $CURRENT_ARCH"
    echo "Defaulting to linux/arm64"
    DOCKER_PLATFORM="linux/arm64"
    ;;
esac

echo "Detected architecture: $CURRENT_ARCH, using Docker platform: $DOCKER_PLATFORM"

PROJECT_NAME="$(
  jq \
    --raw-output \
    '."increase-coverage"."project-name"' \
    test-gen-config.json
)"

TEST_ENV_DOCKER_STAGE="$(
  jq \
    --raw-output \
    '."increase-coverage"."test-env-docker-stage"' \
    test-gen-config.json
)"

docker build \
  --target "$TEST_ENV_DOCKER_STAGE" \
  --platform "$DOCKER_PLATFORM" \
  --tag "$PROJECT_NAME:$TEST_ENV_DOCKER_STAGE" \
  .