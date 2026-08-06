#!/usr/bin/env bash
set -euo pipefail

# Deletes local and remote git tags matching a regex.
# Defaults to dry-run mode to prevent accidental deletion.

REMOTE="origin"
PATTERN='^v0\.0\.'
APPLY=false

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Options:
  --pattern <regex>   Regex to match tags (default: ${PATTERN})
  --remote <name>     Remote name for remote tag deletion (default: ${REMOTE})
  --apply             Actually delete matched tags locally and on remote
  -h, --help          Show this help

Examples:
  # Preview what would be deleted
  ./scripts/delete-test-tags.sh

  # Preview tags matching a custom pattern
  ./scripts/delete-test-tags.sh --pattern '^v0\\.0\\.(10[0-9]|11[0-9])$'

  # Execute deletion
  ./scripts/delete-test-tags.sh --apply
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --pattern)
      PATTERN="${2:-}"
      shift 2
      ;;
    --remote)
      REMOTE="${2:-}"
      shift 2
      ;;
    --apply)
      APPLY=true
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ -z "${PATTERN}" ]]; then
  echo "Error: --pattern cannot be empty." >&2
  exit 1
fi

if [[ "$(git rev-parse --is-inside-work-tree 2>/dev/null || echo false)" != "true" ]]; then
  echo "Error: This script must be run inside a git repository." >&2
  exit 1
fi

TAGS=()
while IFS= read -r tag; do
  TAGS+=("${tag}")
done < <(git tag -l | grep -E "${PATTERN}" || true)

if [[ ${#TAGS[@]} -eq 0 ]]; then
  echo "No tags matched pattern: ${PATTERN}"
  exit 0
fi

echo "Matched ${#TAGS[@]} tag(s) using pattern: ${PATTERN}"
printf '  %s\n' "${TAGS[@]}"

if [[ "${APPLY}" != true ]]; then
  echo
  echo "Dry run only. No tags were deleted."
  echo "Re-run with --apply to delete these tags locally and on remote '${REMOTE}'."
  exit 0
fi

echo
for tag in "${TAGS[@]}"; do
  echo "Deleting local tag: ${tag}"
  git tag -d "${tag}" >/dev/null || true

  echo "Deleting remote tag: ${tag}"
  git push "${REMOTE}" ":refs/tags/${tag}" >/dev/null || true
done

echo
printf 'Deleted %d tag(s) locally and attempted remote delete on %s.\n' "${#TAGS[@]}" "${REMOTE}"
