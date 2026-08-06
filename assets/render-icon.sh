#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SVG_PATH="${SCRIPT_DIR}/icon.svg"
PNG_PATH="${SCRIPT_DIR}/icon.png"
SIZE="128"

if ! command -v sips >/dev/null 2>&1; then
  echo "Error: 'sips' is required on macOS to render icon.png from icon.svg." >&2
  exit 1
fi

if [[ ! -f "${SVG_PATH}" ]]; then
  echo "Error: Source file not found: ${SVG_PATH}" >&2
  exit 1
fi

sips -s format png "${SVG_PATH}" --out "${PNG_PATH}" >/dev/null
sips -z "${SIZE}" "${SIZE}" "${PNG_PATH}" >/dev/null

echo "Rendered ${PNG_PATH} from ${SVG_PATH} (${SIZE}x${SIZE})."
