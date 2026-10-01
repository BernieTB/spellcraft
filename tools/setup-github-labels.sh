#!/usr/bin/env bash
# Create or update the GitHub labels used by the Spellcraft repository.
#
# Prerequisites: GitHub CLI (`gh`) installed and authenticated (`gh auth login`),
# run from inside a clone of the repository so `gh` can detect it.
#
# Usage:
#   tools/setup-github-labels.sh            # create/update labels on GitHub
#   tools/setup-github-labels.sh --dry-run  # print what would happen, no API calls
#
# Idempotent: existing labels are updated in place (`gh label create --force`).
# This script never deletes labels.

set -euo pipefail

# name|colour|description
LABELS=(
  "type:feature|1D76DB|New gameplay or player-facing capability"
  "type:bug|D73A4A|Something is broken"
  "type:tech|5319E7|Refactoring, tooling, infrastructure"
  "type:spike|FBCA04|Time-boxed research or prototype"
  "type:docs|0E8A16|Documentation"
  "area:combat|C5DEF5|Combat simulator and rules"
  "area:cards|C5DEF5|Cards, words, spell line"
  "area:ui|C5DEF5|Interface and feedback"
  "area:content|C5DEF5|Enemies, professors, biomes, data"
  "area:art-audio|C5DEF5|Art and audio"
  "area:tooling|C5DEF5|CI, build, editor tools"
  "epic|3E4B9E|Parent ticket grouping several issues"
  "blocked|B60205|Waiting on something else"
)

dry_run=false
case "${1:-}" in
  "") ;;
  --dry-run) dry_run=true ;;
  -h|--help) sed -n '2,12p' "$0"; exit 0 ;;
  *) echo "Unknown argument: $1 (use --dry-run or --help)" >&2; exit 2 ;;
esac

if [[ "$dry_run" == false ]]; then
  gh auth status >/dev/null 2>&1 || { echo "gh is not authenticated. Run: gh auth login" >&2; exit 1; }
  repo="$(gh repo view --json nameWithOwner --jq .nameWithOwner)"
  echo "Repository: $repo"
fi

for entry in "${LABELS[@]}"; do
  IFS='|' read -r name colour description <<< "$entry"
  if [[ "$dry_run" == true ]]; then
    printf '[dry-run] create/update %-16s #%s  %s\n' "$name" "$colour" "$description"
  else
    gh label create "$name" --color "$colour" --description "$description" --force
  fi
done

echo "Done: ${#LABELS[@]} labels processed."
