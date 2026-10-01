# Spellcraft

![Tests](https://github.com/<user>/<repo>/actions/workflows/tests.yml/badge.svg)
![Unity](https://img.shields.io/badge/Unity-LTS-black?logo=unity)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Status](https://img.shields.io/badge/status-pre--prototype-orange)

**A roguelike auto-battler built around spell-casting.** Compose an incantation out of ancient words, watch it recite itself in a loop while your character fights, and take on the professors of a magic school that doesn't quite take itself seriously.

## Concept

Spellcraft blends two genres: the **idle RPG** (combat plays itself, and you watch your character grow stronger) and the **roguelike** (short runs, build-defining choices, death as a progression engine).

- **A run lasts 20 to 30 minutes** and spans several biomes, each one a year at the school.
- **One character, one class.** It fights the monsters of each biome automatically.
- **Every level-up offers a linked choice:** a passive upgrade paired with a card (a word) to place in your spell line.
- **The spell line is your build.** It is an ordered sequence of short words (2 to 5 letters) that plays on a loop. Cards react to their neighbours and evolve the more they are cast.
- **Goals unlock secret rooms** (e.g. defeating a given number of monsters), where mini-bosses lurk: creatures out of myth.
- **Professors are the bosses.** Before each boss fight, you study your opponent and rearrange your spell line. Once the fight starts, you step back and watch your work unfold.
- **Strict roguelike.** Lose, and the run ends. Meta-progression widens your options (cards, classes, bestiary) without making you mechanically stronger.

See [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md) for the full design.

## Status

Pre-prototype. The repository has just been set up (Sprint 0). Work is tracked in the **Projects** tab of this repository.

## Tech stack

- **Engine:** Unity (LTS version pinned in `ProjectSettings/ProjectVersion.txt`), URP 2D
- **Language:** C#
- **Target platform:** PC (Windows)
- **CI/CD:** GitHub Actions with [GameCI](https://game.ci)
- **Asset versioning:** Git LFS

## Architecture

Game logic is kept separate from Unity so it can be tested and simulated headlessly.

```
Assets/_Project/
├── Core/          # Pure C# logic (combat simulator, cards, effects), no UnityEngine dependency
├── Unity/         # Unity integration: rendering, UI, scenes, ScriptableObjects
└── Tests/         # EditMode tests
docs/              # Game design, architecture decision records (ADRs)
.github/           # CI workflows, issue and PR templates
```

Cards, words, enemies and professors are described by **data** (ScriptableObjects), not code.

## Getting started

### Prerequisites

- [Git](https://git-scm.com) and [Git LFS](https://git-lfs.com)
- [Unity Hub](https://unity.com/download) and the editor version listed in `ProjectSettings/ProjectVersion.txt`, with the *Windows Build Support* module

### Setup

```bash
git lfs install
git clone https://github.com/<user>/<repo>.git
cd <repo>
```

Then open the project folder from Unity Hub. The first import processes all assets and can take a few minutes.

### Running tests

In Unity: **Window > General > Test Runner > EditMode > Run All**.

From the command line:

```bash
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml
```

## Contributing

This is a solo project, developed with a strict workflow.

- One short-lived branch per ticket (`feat/12-spell-line`), never commit directly to `main`.
- Commits follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) (`feat:`, `fix:`, `chore:`, ...).
- One pull request per ticket, and CI must be green before merging.
- The Definition of Done is described in [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Roadmap

| Phase | Goal |
|---|---|
| Sprint 0 | Repository, CI, base architecture |
| Prototype | Automatic combat simulator and spell line |
| Vertical slice | One full biome, one class, one boss, pre-fight preparation |
| MVP | Card pool, meta-progression, first shareable playable build |

Details are tracked in the repository's GitHub Project.

## License

All rights reserved for now. A license will be chosen before any public release.
