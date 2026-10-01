#!/usr/bin/env bash
# Create or update the GitHub milestones used by the Spellcraft repository.
# Milestones are project phases; they have no due date and close when their goal is reached.
#
# Prerequisites: GitHub CLI (`gh`) installed and authenticated (`gh auth login`), `jq` installed,
# run from inside a clone of the repository so `gh` can detect it.
#
# Usage:
#   tools/setup-github-milestones.sh            # create missing milestones, update descriptions
#   tools/setup-github-milestones.sh --dry-run  # print what would happen, no write calls
#
# Idempotent: milestones are matched by exact title, open or closed. Existing ones get their
# description updated (PATCH), missing ones are created (POST). Never deletes or closes anything.

set -euo pipefail

# title|description (description < 250 characters)
MILESTONES=(
  "Sprint 0|Repository, CI and base architecture in place. Done when a trivial PR goes through the full pipeline (issue, branch, PR, green CI, merge) and a Windows build can be downloaded."
  "Prototype|Automatic combat simulator and spell line. Done when seeded fights run headless with a looping card line, neighbour effects and a combat log, covered by tests."
  "Vertical slice|One biome playable end to end: one class, linked level-up choices, one boss with a preparation phase and a recap screen."
  "MVP|First shareable playable build: about ten cards, one biome, one class, one boss, basic meta-progression. Distributed to a few testers."
)

dry_run=false
case "${1:-}" in
  "") ;;
  --dry-run) dry_run=true ;;
  -h|--help) sed -n '2,13p' "$0"; exit 0 ;;
  *) echo "Unknown argument: $1 (use --dry-run or --help)" >&2; exit 2 ;;
esac

command -v jq >/dev/null 2>&1 || { echo "jq is not installed. Install it (e.g. 'winget install jqlang.jq') and retry." >&2; exit 1; }
command -v gh >/dev/null 2>&1 || { echo "gh is not installed. See https://cli.github.com" >&2; exit 1; }
gh auth status >/dev/null 2>&1 || { echo "gh is not authenticated. Run: gh auth login" >&2; exit 1; }

echo "Repository: $(gh repo view --json nameWithOwner --jq .nameWithOwner)"

# All milestones, open and closed. `tr` strips CR added by jq on Windows.
existing="$(gh api --paginate "repos/{owner}/{repo}/milestones?state=all&per_page=100")"

for entry in "${MILESTONES[@]}"; do
  IFS='|' read -r title description <<< "$entry"

  if (( ${#description} >= 250 )); then
    echo "Description of '$title' is ${#description} characters (max 249)." >&2
    exit 1
  fi

  number="$(jq -r --arg t "$title" '.[] | select(.title == $t) | .number' <<< "$existing" | tr -d '\r' | head -n 1)"

  if [[ -n "$number" ]]; then
    if [[ "$dry_run" == true ]]; then
      echo "[dry-run] update #$number '$title'"
    else
      gh api --silent -X PATCH "repos/{owner}/{repo}/milestones/$number" -f description="$description"
      echo "updated #$number '$title'"
    fi
  else
    if [[ "$dry_run" == true ]]; then
      echo "[dry-run] create '$title'"
    else
      gh api --silent -X POST "repos/{owner}/{repo}/milestones" -f title="$title" -f description="$description"
      echo "created '$title'"
    fi
  fi
done

echo "Done: ${#MILESTONES[@]} milestones processed."
