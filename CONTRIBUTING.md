# Contributing

Spellcraft is a solo project developed with Claude Code. This guide is the workflow both follow.
Claude-specific rules are in [`CLAUDE.md`](CLAUDE.md).

## Setup

Follow [Getting started in the README](README.md#getting-started). The Unity project is in the `game/` folder;
open that folder from Unity Hub with editor **6000.3.24f1** (see `game/ProjectSettings/ProjectVersion.txt`).
Run `git lfs install` once per machine before cloning.

## Work tracking

Every change starts from a GitHub Issue, planned in the GitHub Project. Sprints last 2 weeks.

## Branches

- Never commit directly to `main`.
- One short-lived branch per issue, from up-to-date `main`: `type/<issue>-short-name`.
- Types: `feat`, `fix`, `docs`, `chore`, `refactor`, `test`.
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
- CI must be green before merging. (TODO: GameCI workflow not set up yet; until then, run the tests locally
  and state the result in the PR.)
- Merge only after review.

## Testing

- Every change to `Game.Core` comes with EditMode tests in `game/Assets/_Project/Tests/Core/`.
- Bug fixes in Core start with a failing test that reproduces the bug.
- Tests must be deterministic: fixed seeds, no wall-clock time.
- Run the tests before committing:

  ```powershell
  unity test game --editor-version 6000.3.24f1 --mode EditMode --output <scratch-dir>/results.xml
  ```

  This uses the Unity CLI (`unity`, beta). From the editor: **Window > General > Test Runner > EditMode > Run All**.

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
- [ ] Docs updated when relevant (GDD, glossary, ADR, README) and changelog updated when relevant
      (TODO: `CHANGELOG.md` does not exist yet).
- [ ] PR reviewed and merged.
- [ ] Linked issue closed.
