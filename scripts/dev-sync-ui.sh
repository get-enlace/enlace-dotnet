#!/usr/bin/env bash
# Builds @get-enlace/ui locally and copies its dist/ output into
# src/Enlace.AspNetCore/wwwroot-embedded/, so you can sanity-check the real
# embedded-resource path before a release without going through the npm registry.
#
# Usage: scripts/dev-sync-ui.sh [path-to-enlace-ui-checkout]

set -euo pipefail

UI_DIR="${1:-../enlace-ui}"
DEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/src/Enlace.AspNetCore/wwwroot-embedded"

if [[ ! -d "$UI_DIR" ]]; then
  echo "error: enlace-ui checkout not found at '$UI_DIR'" >&2
  echo "usage: $0 [path-to-enlace-ui-checkout]" >&2
  exit 1
fi

echo "Building enlace-ui in $UI_DIR..."
(cd "$UI_DIR" && npm install && npm run build)

if [[ ! -d "$UI_DIR/dist" ]]; then
  echo "error: '$UI_DIR/dist' not found after build" >&2
  exit 1
fi

echo "Syncing $UI_DIR/dist -> $DEST_DIR"
rm -rf "${DEST_DIR:?}"/*
cp -R "$UI_DIR/dist/." "$DEST_DIR/"

echo "Done. Rebuild Enlace.AspNetCore to embed the refreshed bundle."
