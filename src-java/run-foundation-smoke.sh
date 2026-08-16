#!/usr/bin/env sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)

sh "$SCRIPT_DIR/mvnw" -q -DskipTests -pl eshop-foundation-smoke-app -am package
exec java -jar "$SCRIPT_DIR/eshop-foundation-smoke-app/target/app.jar"
