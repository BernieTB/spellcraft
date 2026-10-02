# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
The project has no releases yet and does not follow a versioning scheme. Once the first build is
released, versions will follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Unity 6 project (URP 2D) in `game/`, with Git LFS for binary assets and enforced LF line endings.
- Architecture skeleton: `Game.Core` (pure C# logic), `Game.Unity` (presentation) and `Game.Core.Tests`
  (EditMode tests) assemblies, plus `.editorconfig` code style.
- Game design document, glossary and ADR 0001 (separate core logic from Unity).
- Contributing guide with Git workflow and Definition of Done, and `CLAUDE.md` operating manual.
- GitHub issue forms (feature, bug, tech task, spike, docs) and pull request template.
- Scripts to set up GitHub labels and milestones in `tools/`.
- This changelog.
- CI: EditMode tests on every pull request to `main` (GameCI), required to merge.
- CI: Windows 64-bit player built on every push to `main`, downloadable as a workflow artifact.
- ADR 0002: first-pass combat rules (cast time in ticks, neighbour modifiers, health and shield).
- Core combat model: combatants with health and shield (shield absorbs damage first, healing capped at max
  health, death at zero).
- Data-driven cards: Core card definition (id, cast time, ordered effects) with placeholder deal damage,
  heal and gain shield effects, authored as `CardAsset` ScriptableObjects (ADR 0003).
- Spell line in Core: an ordered sequence of cards with a capacity set from data, played in a loop, whose
  cards can be added, removed, swapped and moved.

### Changed

- Git ignores Claude Code local state (`.claude/worktrees/`, `.claude/settings.local.json`, `CLAUDE.local.md`);
  shared `.claude/` config can still be committed.

### Changed

- CI: both workflows log the runner's free disk space after cleanup and after the Unity step.
- CI: Git LFS objects are cached between runs, so each one is downloaded only once.

[Unreleased]: https://github.com/BernieTB/spellcraft/commits/main
