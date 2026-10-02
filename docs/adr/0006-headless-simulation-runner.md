# 0006. Headless simulation runner

- **Status:** Proposed
- **Date:** 2026-10-02

## Context

Balancing is done by simulation ([ADR 0001](0001-separate-core-logic-from-unity.md)): we need to run many
seeded fights without a window, get a summary file (win rate, fight length, damage per card) and get the same
summary for the same seeds (issue #15).

The combat rules are in `Game.Core` and run headless. The fight content is not: hero, enemies, cards and the
tick limit are authored as Unity assets (`CardAsset`, `DebugFightSetup`) and only become Core data through
`Game.Unity` mapping code, which needs the Unity asset database to load.

Two ways to run Core in bulk without a window:

- the Unity editor in batch mode (`Unity.exe -batchmode -quit -executeMethod ...`), already used and verified
  for the placeholder card generator and the debug scene builder;
- a separate .NET console project compiling the Core sources (the .NET 6 SDK is available).

## Decision

We will run simulations in the Unity editor in batch mode, through an editor-only entry point,
`Game.Unity.EditorTools.Simulation.SimulationRunner.Run`, called with `-executeMethod`.

- **Input:** a `DebugFightSetup` asset (default: the debug fight's `DebugFightSetup.asset`), so the fight is the
  same data the debug viewer replays. The runner replaces the asset's single seed with a range: fight `i` uses
  seed `firstSeed + i`. Options come from the editor command line (`-simSetup`, `-simFights`,
  `-simSeedStart`, `-simOutput`), read with `Environment.GetCommandLineArgs()`. A tool default of 1,000 fights
  is the only number in code; every fight number comes from the asset.
- **Logic:** the batch loop and the aggregation are in Core (`Game.Core.Simulation`: `FightBatch`,
  `SimulationSummaryBuilder`, `SimulationSummary`), covered by EditMode tests. They take a factory that builds a
  fresh `Fight` for a seed, so Core still knows nothing of assets. The Unity entry point only loads the asset,
  times the run and writes the file.
- **Output:** one JSON file, hand-written with invariant culture and a fixed field order (reusing the combat
  log's string escaping): fight count, seed range, hero / enemies / timeout counts and rates, average, minimum,
  maximum and total ticks, and per side and card id: casts, damage, health lost, shield absorbed, average
  damage per fight, healing and shield gained. **Damage** is the damage actually applied: health lost plus
  damage absorbed by the target's shield (overkill excluded), read from the cast effect outcomes, neighbour
  bonuses included. The summary holds no timing, so it is identical byte for byte for the same seeds.
  Durations are written to the editor log instead.
- **Location:** by default the file goes to `game/SimulationResults/simulation-summary.json`, which Git
  ignores; `-simOutput` can point anywhere else. Run outputs are never committed.

## Consequences

### Positive

- No second build system: the runner uses the project's assemblies and assets as they are, so a simulation
  always uses the current content and the same mapping code as the game.
- The same entry point works from the menu (**Tools > Game > Run Simulation**) and headless, and can later run
  in CI with the GameCI image already used for tests.
- Core gains a reusable, tested batch and summary API that any later runner (CI job, console app) can call.

### Negative

- Each run pays the editor's start-up time (several seconds, more on a cold `Library`), far more than the
  simulation itself for small batches.
- The editor must be closed on the project (Unity locks a project to one instance), and a batch run needs a
  Unity license.
- The input is the debug fight setup type, which describes one fixed encounter. Sweeping several spell lines or
  enemy groups needs a dedicated setup asset later.

## Alternatives considered

- **.NET console project compiling Core:** fast start-up and no license, but the content is authored as Unity
  assets, so it would need either a second, Unity-free content format or a YAML asset reader, plus a second
  build to keep in sync with the asmdefs. Worth revisiting if start-up time or licensing become a problem.
- **A dedicated `SimulationSetup` asset:** cleaner long term, but it would duplicate `DebugFightSetup` today.
  Deferred until simulations need more than one encounter.
- **CSV output:** easy to open in a spreadsheet, but the per-card table and the global figures do not fit one
  flat table. JSON keeps them in one file.
