# 0005. Neighbour modifier resolution

- **Status:** Proposed
- **Date:** 2026-10-02

## Context

[ADR 0002](0002-first-pass-combat-rules.md) lets a card carry **neighbour modifiers** for the next or previous
card of its own spell line. The owner confirmed its details on 2026-10-02
([ADR 0004](0004-confirm-first-pass-combat-rules.md)): a modifier applies to one cast of the neighbour, then is
used up; several modifiers on the same cast add up; in a single-card line the card is its own neighbour. The
prototype kinds are **+X damage, +X heal and +X shield**, each with its own direction (next or previous) and X
from data. "A bonus only adds to effects of the same kind on the receiving card"; how much it adds when that card
has several effects of the kind is not specified. A "previous" bonus applies to the previous position's next
cast, so on the next loop.

[ADR 0003](0003-effect-representation.md) requires that modifiers are applied without mutating effects or card
definitions (they are shared and immutable), that pending modifiers belong to a spell-line position rather than
to a card (the same card can sit at several positions), and leaves the choice of mechanism to #13: either
`EffectContext` carries the active modifiers, or the resolver builds a modified copy of the card's effects.

## Decision

We will carry the bonus of a cast in **`EffectContext`** and let effects read it.

- **Data.** `NeighbourModifier` (`Game.Core.Cards`) holds a `BonusKind` (damage, heal, shield), a
  `NeighbourDirection` (next, previous) and an amount (zero or more), validated in its constructor.
  `CardDefinition.NeighbourModifiers` lists them. In Unity, `CardAsset` has a list of `NeighbourModifierEntry`
  (kind, direction, amount) converted to Core. Both enums have explicit values and are never renumbered.
- **Pending state.** The fight keeps one `EffectBonus` (a damage, heal and shield value) per combatant and per
  spell-line position. Nothing is stored on cards or effects.
- **Order within a cast.** When a card resolves, the fight (1) takes and clears the bonus waiting on its
  position, (2) resolves the card with that bonus in its `EffectContext`, then (3) adds each of the card's
  modifiers to the bonus waiting on `PositionAfter` or `PositionBefore` of its position. Taking before granting
  makes a single card boost its own next cast instead of its current one.
- **Applying.** `DealDamageEffect`, `HealEffect` and `GainShieldEffect` add `context.ConsumeBonus(kind)` to their
  amount. The first effect of a kind takes the whole bonus of that kind; later effects of the same kind in that
  cast get nothing. A bonus with no matching effect is never consumed and is lost with the cast. Outcomes
  report the boosted amounts.
- **Dead casters** stop casting, so bonuses waiting in their line are never used and never move to another
  combatant.

## Consequences

### Positive

- Effects and card definitions stay immutable and shared; the fight owns all per-position state.
- A new effect kind opts in to bonuses with one line in its `Apply`; the resolver does not need to know
  concrete effect types.
- Outcomes and the cast records already include bonuses, so the combat log (#14) and recap see them.

### Negative

- "Once per cast, on the first effect of that kind" is an interpretation: on a card with two damage effects, a
  +X damage bonus adds X once, not X per effect. If the owner prefers X per effect, only `EffectContext` changes.
- `EffectContext` is now stateful (the remaining bonus), so it must not be reused across casts.
- Cast records do not say which part of an outcome came from a bonus. The combat log may need the bonus of each
  cast (available as `EffectContext.Bonus`) to explain it.

## Alternatives considered

- **Resolver builds a modified copy of the card's effects:** needs the resolver to know every effect type and
  how to rebuild it with a new amount, and allocates new effects on every boosted cast.
- **Bonus applied to every effect of the matching kind:** simple, but a card's bonus would scale with how its
  effects are split, which data authors would not expect from "the next card deals +X damage".
- **Pending modifiers stored on `CardDefinition`:** breaks when the same card is at two positions or in two
  lines, and mutates shared data.
