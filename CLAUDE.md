# CLAUDE.md

Operating manual for Claude Code in this repository. Follow it strictly.

## Project

Spellcraft (working title) is a solo-developed roguelike auto-battler for Windows PC. The player builds a
**spell line**, an ordered loop of short magic words (cards), and watches a single character fight through
the biomes of a magic school, each biome ending with a professor boss. The full design is in
[`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md). Terms are defined in [`docs/GLOSSARY.md`](docs/GLOSSARY.md).

## Stack

- Unity **6000.3.24f1** (LTS, pinned in `game/ProjectSettings/ProjectVersion.txt`), URP 2D, C#.
- Target: Windows PC only.
- Git LFS for images, audio, fonts and 3D models (see `.gitattributes`).
- CI/CD: GitHub Actions with GameCI. `.github/workflows/tests.yml` runs the EditMode tests on every PR to
  `main` (required check `EditMode tests` in the `main` repository ruleset, see
  [GitHub settings](CONTRIBUTING.md#github-settings)); `.github/workflows/build.yml` builds on push to `main`.
- Sprints of 2 weeks, tracked in GitHub Issues + GitHub Projects.

## Layout

The Unity project lives in `game/`, not at the repo root.

| Folder | Assembly | Role |
|---|---|---|
| `game/Assets/_Project/Core/` | `Game.Core` | Pure C# game logic (combat simulator, cards, effects) |
| `game/Assets/_Project/Unity/` | `Game.Unity` | Rendering, UI, scenes, ScriptableObjects. References `Game.Core` |
| `game/Assets/_Project/Tests/Core/` | `Game.Core.Tests` | EditMode tests for `Game.Core` (Editor only, NUnit) |

`docs/` holds design docs and ADRs. Do not put project code outside `game/Assets/_Project/`.

## Architecture rules (hard constraints)

Rationale: [ADR 0001](docs/adr/0001-separate-core-logic-from-unity.md).

1. **No `UnityEngine` or `UnityEditor` in Core.** `Game.Core.asmdef` has `noEngineReferences: true`; keep it.
   Never add Unity types (`Vector2`, `Mathf`, `Debug`, `ScriptableObject`...) to Core.
2. **No game logic in MonoBehaviours.** They read input, call Core, and display Core state. Rules, damage,
   card effects and targeting live in Core.
3. **Determinism.** The combat simulation must give identical results for identical inputs and seed:
   - inject a seeded RNG abstraction into Core; never use `UnityEngine.Random`, a static/shared
     `System.Random`, or `Guid.NewGuid()` for gameplay;
   - never read wall-clock or frame time in Core (`DateTime.Now`, `Stopwatch`, `Time.time`); advance the
     simulation in discrete ticks;
   - never iterate unordered collections (`HashSet`, `Dictionary`) where order affects the outcome.
4. **Headless.** Core must run without a scene, a frame loop or the editor, so fights can be simulated in bulk.
5. **Data-driven content.** Cards, words, enemies and professors are ScriptableObjects in `Game.Unity`, mapped
   to plain Core data. Never hardcode a specific card, enemy or professor in code.
6. **No hardcoded balance numbers.** Damage, HP, costs, XP curves and probabilities come from data assets.
7. **Prefer code and data over scenes and prefabs.** You cannot see the editor and YAML is fragile. Keep
   scenes small and prefabs modular. If a change needs editor work, say so and describe the exact steps
   instead of hand-editing scene/prefab YAML.

## Commands

Run EditMode tests (verified, uses the Unity CLI `unity`, beta):

```powershell
unity test game --editor-version 6000.3.24f1 --mode EditMode --output <scratch-dir>/results.xml
```

Exit code `0` = all passed, `8` = tests failed, anything else = the run itself failed (compile error,
license, crash). Write results outside the repo.

- TODO: raw editor command line (`Unity.exe -batchmode -runTests ...`), not verified yet.
  Editor path on the dev machine: `C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Unity.exe`.
- CI: `.github/workflows/tests.yml` (GameCI `unity-test-runner`, `testMode: EditMode`, `projectPath: game`,
  Unity version read from `ProjectVersion.txt`) runs on PRs to `main` and on manual dispatch
  (`gh workflow run tests.yml --ref <branch>`). Results are uploaded as the `editmode-test-results` artifact.
  Needs the `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` repository secrets.
  `game/Library` is cached with the key `Library-EditMode-<hash of Assets, Packages, ProjectSettings>`; on a
  miss it falls back to any `Library-` cache, such as the one `build.yml` saves on `main`.

## Code conventions

- Namespaces mirror assemblies and folders: `Game.Core.*`, `Game.Unity.*`, `Game.Core.Tests.*`.
- Formatting and naming follow `.editorconfig` (LF, 4 spaces, Allman braces, PascalCase types/members,
  `_camelCase` private fields, `I`-prefixed interfaces, explicit access modifiers).
- A new module gets its own folder and `.asmdef` under `_Project/`. Dependencies point toward Core only;
  Core references nothing in the project.
- Every new public Core behaviour gets EditMode tests in `Game.Core.Tests`.
- Tests are named `Method_Condition_Expected` or `Subject_Behaviour`, one behaviour per test.

## Git workflow

- **Never commit to `main`.** Branch from up-to-date `main`.
- Branch name: `type/<issue>-short-name` (e.g. `feat/12-spell-line`). Types: `feat`, `fix`, `docs`, `chore`,
  `refactor`, `test`, `spike`. Never merge a `spike/` branch: report findings in the issue or an ADR.
- Commits: [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/), one logical change each.
- One PR per ticket, body contains `Closes #<issue>`. Never merge a PR yourself unless asked.
- A PR missing the required check or behind `main`: `gh pr update-branch <number>` (no Update branch button;
  see [GitHub settings](CONTRIBUTING.md#github-settings)).
- **Run the EditMode tests before every commit** that touches code. Do not commit with failing tests.
- Do not push or open PRs without the owner's go-ahead unless the task says so.
- Full workflow and Definition of Done: [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Never

- Edit `.meta` files by hand, or delete/recreate them (GUIDs break references). Let Unity generate them.
- Commit `game/Library/`, `game/Temp/`, `game/Logs/`, `game/UserSettings/`, `game/Builds/`, `*.ulf`, `*.alf`,
  `.env` or any secret. Check `git status` before every commit.
- Change files in `game/ProjectSettings/` without stating why in the commit message.
  (Unity may rewrite settings or URP assets on upgrade; that is fine, but call it out.)
- Invent final game words, word lists, card names or professor names. The owner writes them. Use obvious
  placeholders (`WORD_A`, `TestCard1`) and say so.
- Use proper names, invented terms or recognisable visuals from protected works (Harry Potter, Naruto...).
- Bypass Git LFS for binary assets or skip git hooks.

## Ask before deciding

If a task touches anything listed in **Open questions** in [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md#open-questions)
(class identity, elements and neighbour rules, pacing and sizes, bosses, audio, title, art pipeline),
stop and ask. Do not pick a design and build on it silently. The same applies to any mechanic not described
in the GDD.

When a structural decision is made, record it as an ADR in [`docs/adr/`](docs/adr/README.md).

## References

- [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md): game design (source of truth for mechanics)
- [`docs/GLOSSARY.md`](docs/GLOSSARY.md): game terms
- [`docs/adr/`](docs/adr/README.md): architecture decision records
- [`CONTRIBUTING.md`](CONTRIBUTING.md): workflow, conventions, Definition of Done
