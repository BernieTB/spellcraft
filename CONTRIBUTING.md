# Contributing

Spellcraft is a solo project developed with Claude Code. This guide is the workflow both follow.
Claude-specific rules are in [`CLAUDE.md`](CLAUDE.md).

## Setup

Follow [Getting started in the README](README.md#getting-started). The Unity project is in the `game/` folder;
open that folder from Unity Hub with editor **6000.3.24f1** (see `game/ProjectSettings/ProjectVersion.txt`).
Run `git lfs install` once per machine before cloning.

## Work tracking

Every change starts from a GitHub Issue, planned in the GitHub Project. Sprints last 2 weeks.

- Milestones represent project phases (Sprint 0, Prototype, Vertical slice, MVP). They have no due date and
  close when their goal is reached. Setup: `tools/setup-github-milestones.sh`.
- Sprints are tracked with the Iteration field of the GitHub Project, not with milestones.

## Branches

- Never commit directly to `main`.
- One short-lived branch per issue, from up-to-date `main`: `type/<issue>-short-name`.
- Types: `feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `spike`.
- `spike/...` branches hold throwaway prototypes for a spike issue. They are never merged; the findings go
  in the issue or in an ADR.
- Example: `feat/12-spell-line`.

## Commits

Use [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/):

```
<type>(<optional scope>): <short imperative summary>
```

Examples: `feat(core): add spell line loop`, `fix(unity): keep card order on reload`, `docs: update GDD`.
Keep one logical change per commit. Mention any change to `game/ProjectSettings/` and why.

## Pull requests

- One PR per issue. The PR body contains `Closes #<issue>`.
- Keep PRs small enough to review in one sitting.
- CI must be green before merging. The `Tests` workflow (`.github/workflows/tests.yml`) runs the EditMode
  tests with GameCI on every PR to `main`; superseded runs are cancelled and the results are attached to the
  run as an artifact. It can also be started by hand from the Actions tab (`workflow_dispatch`). A separate
  build workflow (`.github/workflows/build.yml`) runs on push to `main`.
- Merge only after review.

### GitHub settings

- `main` is protected by a ruleset: pull request required (0 approvals), required check `EditMode tests`, no
  force push, no deletion.
- Rulesets are only enforced on public repositories with GitHub Free. The repository is public for that
  reason: making it private silently disables the protection, unless the account moves to GitHub Pro.
- The ruleset does not require branches to be up to date, so GitHub shows no **Update branch** button. To
  bring a PR branch up to date with `main` (for example a PR opened before a required workflow existed, which
  never gets the required check), run `gh pr update-branch <number>`; this re-triggers the checks.

## Changelog

[`CHANGELOG.md`](CHANGELOG.md) follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

- Update the `## [Unreleased]` section in the same PR as any notable change: new features, bug fixes,
  removals, and tooling or workflow changes visible to the developer.
- Add one short line under the matching heading (`Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`,
  `Security`), written for a player or developer, without commit hashes.
- Skip it for typo fixes, internal refactors and other changes nobody would notice.

## Testing

- Every change to `Game.Core` comes with EditMode tests in `game/Assets/_Project/Tests/Core/`.
- Bug fixes in Core start with a failing test that reproduces the bug.
- Tests must be deterministic: fixed seeds, no wall-clock time.
- Golden tests compare a fight against a stored trace (e.g.
  `game/Assets/_Project/Tests/Core/Combat/Golden/basic_fight.txt`). When a rule change alters the outcome on
  purpose, regenerate the file with the `[Explicit]` test `Run_GoldenFight_RegenerateStoredTrace` (Test Runner:
  select it and Run Selected, or add `--filter Run_GoldenFight_RegenerateStoredTrace` to the command below),
  review the diff line by line and commit it with the change. Never regenerate just to make a failure go away.
  The combat log has its own golden file (`Golden/basic_fight_log.txt`), regenerated the same way with
  `Record_GoldenFight_RegenerateStoredLog`.
- Run the tests before committing:

  ```powershell
  unity test ./game --editor-version 6000.3.24f1 --mode EditMode --output <scratch-dir>/results.xml
  ```

  This uses the Unity CLI (`unity`, beta). Keep the `./` so `game` is read as a path, not a project name.
  From the editor: **Window > General > Test Runner > EditMode > Run All**.

## Code style

- Formatting and naming are defined in [`.editorconfig`](.editorconfig): LF, 4 spaces, Allman braces,
  PascalCase for types and members, `_camelCase` for private fields, `I` prefix for interfaces.
- Namespaces mirror assemblies: `Game.Core.*`, `Game.Unity.*`, `Game.Core.Tests.*`.
- Architecture rules (no `UnityEngine` in Core, determinism, data-driven content) are in
  [`CLAUDE.md`](CLAUDE.md#architecture-rules-hard-constraints) and
  [ADR 0001](docs/adr/0001-separate-core-logic-from-unity.md).

## Definition of Done

An issue is done when all of these are true:

- [ ] Tests written and passing.
- [ ] CI green.
- [ ] No `UnityEngine` reference in `Game.Core`.
- [ ] No game logic in MonoBehaviours.
- [ ] No hardcoded balance numbers (content is data-driven).
- [ ] No secrets, `.ulf`/`.alf` files or generated Unity folders committed.
- [ ] Docs updated when relevant (GDD, glossary, ADR, README) and changelog updated when relevant
      (see [Changelog](#changelog)).
- [ ] PR title follows Conventional Commits.
- [ ] PR reviewed and merged.
- [ ] Linked issue closed.
