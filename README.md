# Spellcraft

[![Tests](https://github.com/BernieTB/spellcraft/actions/workflows/tests.yml/badge.svg)](https://github.com/BernieTB/spellcraft/actions/workflows/tests.yml)
![Unity](https://img.shields.io/badge/Unity-6000.3%20LTS-black?logo=unity)
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

- **Engine:** Unity 6000.3.24f1 LTS (pinned in `game/ProjectSettings/ProjectVersion.txt`), URP 2D
- **Language:** C#
- **Target platform:** PC (Windows)
- **CI/CD:** GitHub Actions with [GameCI](https://game.ci) (EditMode tests on every pull request)
- **Asset versioning:** Git LFS

## Architecture

Game logic is kept separate from Unity so it can be tested and simulated headlessly.

```
game/                       # Unity project (open this folder in Unity Hub)
└── Assets/_Project/
    ├── Core/               # Game.Core: pure C# logic (combat simulator, cards, effects), no UnityEngine dependency
    ├── Unity/              # Game.Unity: rendering, UI, scenes, ScriptableObjects
    └── Tests/Core/         # Game.Core.Tests: EditMode tests for Core
docs/                       # Game design, glossary, architecture decision records (ADRs)
CLAUDE.md                   # Operating manual for Claude Code
CONTRIBUTING.md             # Workflow, conventions, Definition of Done
```

The reasoning behind this split is in [ADR 0001](docs/adr/0001-separate-core-logic-from-unity.md).

Cards, words, enemies and professors are described by **data** (ScriptableObjects), not code.

## Getting started

### Prerequisites

- [Git](https://git-scm.com) and [Git LFS](https://git-lfs.com)
- [Unity Hub](https://unity.com/download) and the editor version listed in `game/ProjectSettings/ProjectVersion.txt` (currently 6000.3.24f1), with the *Windows Build Support* module

### Setup

```bash
git lfs install
git clone https://github.com/BernieTB/spellcraft.git
cd spellcraft
```

Then, in Unity Hub, **add the `game/` folder** (not the repository root) and open it. The first import processes
all assets and can take a few minutes.

### Running tests

In Unity: **Window > General > Test Runner > EditMode > Run All**.

From the command line, from the repository root, with the Unity CLI (`unity`, beta):

```bash
unity test ./game --editor-version 6000.3.24f1 --mode EditMode --output <scratch-dir>/results.xml
```

Exit code `0` means all tests passed, `8` means tests failed, anything else means the run itself failed.
Write the results file outside the repository.
Keep the `./`: a bare `game` is also read as a project name and can test another project called `game`.

### Watch a simulated fight

A debug scene replays a fight simulated by the combat code, with plain placeholder UI (no final art). It is an
editor tool and is not part of the game build.

1. Open the `game/` project in Unity.
2. In the **Project** window, open `Assets/_Project/Unity/DebugTools/DebugFight.unity` (double-click it).
3. Press **Play**. The fight plays in the **Game** view; its full text log is also printed in the **Console**.

Controls: **Space** pause/play, **Right arrow** step to the next tick with events, **R** restart, **+** / **-**
(or the slider) change the speed from x0.25 to x8. **Reload setup** simulates again, **Copy JSON** copies the
combat log to the clipboard. Keyboard shortcuts need the Game view to have focus (click it once).

The fight comes from `Assets/_Project/Unity/DebugTools/DebugFightSetup.asset`: select it to change the hero and
enemies (health, shield, spell line of card assets), the seed, the tick limit and the playback rate (ticks per
second at x1). Edits are picked up the next time you press Play, or with **Reload setup** while playing. If the
scene or the asset is missing, use **Tools > Game > Rebuild Debug Fight Scene** (it keeps an existing setup
asset).

### Simulate many fights

The headless simulation runner plays the fight of `DebugFightSetup.asset` (see above) over a range of seeds and
writes a JSON summary: how many fights the hero won, lost or timed out (counts and rates), the average, minimum and
maximum fight length in ticks, and per card and side the casts, damage (health lost + shield absorbed), healing and
shield gained. The same seeds always give the same file. Close the editor on the project first, then from PowerShell:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Unity.exe" -batchmode -quit -projectPath <absolute path to game> `
  -executeMethod Game.Unity.EditorTools.Simulation.SimulationRunner.Run `
  -simFights 1000 -simSeedStart 1 -simOutput <scratch-dir>/simulation-summary.json -logFile <scratch-dir>/simulation.log
```

All `-sim*` options are optional: `-simSetup` (another `DebugFightSetup` asset), `-simFights` (default 1000),
`-simSeedStart` (default: the asset's seed) and `-simOutput` (default `game/SimulationResults/simulation-summary.json`,
ignored by Git; a relative path is resolved from `game/`). The exit code is `0` on success and `1` on error; timings
are printed in the log. In the editor, use **Tools > Game > Run Simulation**. Design notes:
[ADR 0006](docs/adr/0006-headless-simulation-runner.md).

### Downloading a build

Every push to `main` builds the Windows player (workflow **Build**, which can also be run manually). Open the
run in the **Actions** tab and download the `Spellcraft-Windows64-<short-sha>` artifact. Artifacts are kept for
7 days. Unzip it and run `Spellcraft.exe`. The next section explains how to play.

### Play the Vertical slice

The build holds the whole Vertical slice (#88): one class (`CLASS_A`), one biome (`BIOME_01`) and one professor, all
with working names and placeholder visuals (flat shapes), played from the title screen to the end of the run.

1. Download the artifact (see above), unzip it, run `Spellcraft.exe` (windowed, 1920x1080 reference; the UI scales).
2. **Title**: **New run** starts a run with a seed taken from the clock (shown on the end screen, useful in bug
   reports); **Quit** closes the game.
3. **Run screen**: pick the next step with the buttons: a regular fight, an unlocked secret room (mini-boss) or, after
   enough regular fights, the professor. The fight plays by itself; use **Pause** and the speed button
   (x1/x2/x4/x8). During a regular fight, click a line slot, then another slot or a reserve card, to move or swap
   cards live. **Leave** abandons the fight (it does not count).
4. **Level-up**: each level gives a mandatory choice of one of three packages (a card and a passive); no fight can
   start before it. With a full line, send the new card to the reserve or replace a line card.
5. **Mini-boss and professor**: the preparation screen lets you arrange the line and the reserve (the line is fixed
   once the fight starts). The first win in a secret room reveals part of the professor to the **bestiary**.
6. **Recap** after every fight, then the **end screen** (victory or defeat) and back to the title.

The bestiary is the only save. It is loaded at startup, saved after every reveal and kept between runs, in
`%USERPROFILE%\AppData\LocalLow\DefaultCompany\game\bestiary.json` (company and product name are the placeholder
values of the Unity player settings; the game logs the exact path at startup in
`%USERPROFILE%\AppData\LocalLow\DefaultCompany\game\Player.log`, and in the editor the path is under
`%USERPROFILE%\AppData\LocalLow\DefaultCompany\game\` too). To reset what the game knows, close the game and
delete `bestiary.json` and any `bestiary.unreadable*.json` next to it. There is no other save: a run is lost if the
window is closed.

In the editor, open `game/Assets/_Project/Unity/Scenes/Bootstrap.unity` and press Play: it is the same flow.

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
