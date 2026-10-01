# 0001. Separate core game logic from Unity

- **Status:** Accepted
- **Date:** 2026-10-01

## Context

Spellcraft's combat is automatic: the outcome depends only on the spell line, the character and the enemy.
The design requires that:

- **Balancing is done by simulation.** Thousands of fights must run headless, including in CI, to tune cards
  and enemies.
- **Combat is deterministic.** The same inputs and seed must give the same result, so fights can be replayed,
  the post-fight recap is trustworthy, and simulations are reproducible.
- **Logic is testable.** Rules for neighbour effects and card evolution are intricate and must be covered by
  fast automated tests.

Code that depends on `UnityEngine` needs the engine to run, tends to rely on frame time and global state
(`Time.time`, `UnityEngine.Random`), and is slow to test.

## Decision

We will keep all game rules in a Unity-independent assembly, `Game.Core`
(`game/Assets/_Project/Core/Game.Core.asmdef`, with `noEngineReferences: true`), so the compiler rejects any
`UnityEngine` reference.

- `Game.Unity` (`game/Assets/_Project/Unity/`) references `Game.Core` and acts as an adapter: it turns
  ScriptableObject data into Core data, drives the simulation and displays its state. It contains no game rules.
- `Game.Core.Tests` (`game/Assets/_Project/Tests/Core/`) holds EditMode tests for Core.
- Core receives randomness through an injected, seeded RNG and advances time in discrete steps. It never reads
  wall-clock or frame time.

## Consequences

### Positive

- Core runs without a scene or frame loop, so mass simulations are possible headless and in CI.
- EditMode tests for Core are fast and do not need Play mode.
- Determinism is enforceable: the forbidden APIs are not reachable from Core.
- The compiler enforces the boundary, not just a convention.

### Negative

- A mapping layer is needed between ScriptableObjects (Unity) and plain Core data types.
- Core cannot use Unity types (`Vector2`, `Mathf`, `Debug.Log`); equivalents must be written or taken from .NET.
- Two representations of some concepts (data asset and Core model) must be kept in sync.

## Alternatives considered

- **Logic in MonoBehaviours:** fastest to start, but untestable without Play mode, not runnable headless, and
  prone to non-determinism through frame time and global random.
- **Separation by convention only (no asmdef constraint):** no compiler check, so `UnityEngine` usage would leak
  into Core over time.
- **External .NET library outside the Unity project:** strongest isolation, but adds a build step and DLL
  management for a solo project. Can be revisited if Core needs to run outside Unity.
