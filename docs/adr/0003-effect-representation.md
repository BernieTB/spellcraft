# 0003. Card effect representation

- **Status:** Proposed
- **Date:** 2026-10-01

## Context

A card must be described as data and executed by the headless Core simulator
([ADR 0001](0001-separate-core-logic-from-unity.md)). A card does one or more things when it resolves (deal
damage, heal, gain shield...). The first-pass combat rules (ADR 0002, first-pass combat rules) add constraints:

- combatants have health and shield;
- each card has a cast time in simulation ticks, read from data;
- neighbour effects are carried by a card and modify the next (or previous) card, for example "the next card
  deals +X damage". They are implemented later (#13), but the effect model must not block them.

Content is authored as ScriptableObjects in `Game.Unity` and converted to plain Core data. Claude Code cannot
see the inspector, so authoring must work with Unity's default inspector and plain serialized fields.

## Decision

We will represent effects in Core as **small immutable objects implementing `IEffect`**
(`Game.Core.Effects`), one class per effect kind, each with its parameters (amounts) passed in from data and
validated in its constructor. `IEffect.Apply(EffectContext)` performs the effect against an `EffectContext`
that currently holds only the caster and the target. A `CardDefinition` (`Game.Core.Cards`) holds an id, a cast
time and an ordered list of `IEffect`, and `Resolve` applies them in order.

In the Unity layer we will author effects as a **flat list of `EffectEntry` (kind enum + amount)** on a
`CardAsset` ScriptableObject. `CardAsset.ToDefinition()` maps each entry to its Core effect with one `switch`.
`EffectKind` values are explicit integers that are never renumbered, because assets serialize them as
integers.

Forward-looking constraint for neighbour modifiers (#13): modifiers must be applicable **without mutating** an
effect or a definition, which are shared and immutable. The intended extension point is the resolution step:
either `EffectContext` carries the modifiers active for the card being resolved and effects read their final
amount through it, or the resolver builds a modified copy of the card's effects. Effects that can be modified
(e.g. damage) will expose their parameters for that purpose, as `Amount` does today. Choosing between these is
left to #13.

## Consequences

### Positive

- Adding an effect kind is local: one Core class with its tests, one enum value and one `switch` case.
- Effects are plain C#, testable in isolation, deterministic and free of Unity types.
- The authoring format works with the default inspector: no custom editor, no `[SerializeReference]` type
  picker to maintain.
- Validation lives in Core constructors, so invalid data fails at conversion time with a clear message, not
  mid-fight.

### Negative

- Every effect entry has the same fields (kind + one amount). Effects that need different parameters (a target
  selector, a duration, a tag) will need more fields on `EffectEntry` or a move to polymorphic authoring.
- The kind enum and the Core classes must be kept in sync by hand (covered by conversion tests).
- Who an effect acts on is fixed per kind for now (damage hits the target, heal and shield go to the caster).
  A data-driven target selector can be added when a card needs it.

## Alternatives considered

- **Single data struct in Core with a kind enum and a big `switch` in the simulator:** easy to serialize, but
  every new effect touches one central function and its tests, and per-kind parameters pile up as unused fields.
- **`[SerializeReference]` list of polymorphic effect data in the ScriptableObject:** mirrors Core one-to-one
  and allows per-kind fields, but Unity has no built-in type picker for it, so it needs a custom inspector, and
  renaming a class breaks serialized assets. Worth revisiting once effects have heterogeneous parameters.
- **One ScriptableObject asset per effect, referenced by cards:** flexible and inspector-friendly, but doubles
  the number of assets for simple cards and makes content harder to review in diffs.
