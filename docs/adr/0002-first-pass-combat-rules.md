# 0002. First-pass combat rules: cast time, card-carried neighbour effects, health and shield

- **Status:** Accepted
- **Date:** 2026-10-01

## Context

Spike #8 asked three questions that block the combat simulator (#11, #12, #13), all listed as open questions
in the GDD:

1. How are cards resolved over time in the looping spell line?
2. Which neighbour-effect rules and tags exist for the first prototype?
3. Which stats do the first cards act on?

Constraints: the simplest model that works for a prototype; deterministic and headless
([ADR 0001](0001-separate-core-logic-from-unity.md)); data-driven, with no balance numbers in code; and
consistent with the pillars "the spell line is the build" and "preparation over luck". The project owner
decided each point on 2026-10-01 from options compared during the spike. These rules are a first pass, to be
revised after playtests of the prototype.

## Decision

**Timing: cast time in ticks.**

- A fight advances in discrete **ticks**. Nothing reads wall-clock or frame time.
- Every card has a **cast time**: a positive whole number of ticks, read from the card data.
- Each combatant casts the cards of its spell line one after the other, in order, looping from the last card
  back to the first. A card's effects resolve when its cast time has elapsed; the next card starts casting
  on the following tick.
- When several casts complete on the same tick, they resolve in a fixed order: the hero first, then enemies
  in their order of appearance. This tie-break keeps the simulation deterministic and may be revised.

**Enemies use the same system.** An enemy has its own spell line and casts it exactly like the hero. There is
one combat engine for everybody, and professors will later "cast spells" too.

**Neighbour effects: carried by the card.**

- A card may carry **neighbour modifiers** that apply to the next card or the previous card in its own spell
  line, for example "the next card deals +X damage". X comes from the card data.
- "Next" and "previous" follow the loop: the next card after the last one is the first one. In a single-card
  line, the card's neighbour is itself.
- A modifier applies to one cast of the neighbour card, then is used up. Modifiers on the same cast add up.
- There is no tag or element catalogue in the first prototype. Tags, elements and grammar-like roles remain
  open questions.

**Stats: health and shield.**

- **Health:** a combatant at zero health is dead and the fight ends.
- **Shield:** absorbs damage before health. Damage first reduces shield; only the remainder reduces health.
  Shield has no cap and does not decay in the first prototype.
- The first placeholder effects are deal damage, heal (restores health, capped at maximum health) and
  gain shield. Starting health, maximum health and starting shield come from data.

## Consequences

### Positive

- Cast time gives each card a second dimension (speed versus power), and leaves room to tie it to word length
  and evolution later.
- Card-carried modifiers make position matter without deciding the element catalogue.
- One engine for hero and enemies keeps the simulator small and makes boss behaviour data-driven.
- Shield creates an attack-versus-defence choice with a single extra stat.

### Negative

- The same-tick tie-break favours the hero; this must be watched during balancing.
- Modifiers that target "previous" only affect the next loop of the line, which can be less readable; the
  combat log and recap must make it visible.
- Every rule here is provisional and expected to change after playtests (each change needs a new ADR).

## Alternatives considered

- **Alternating turns:** simplest, but order would be the only decision dimension and cards could not trade
  speed for power.
- **Speed gauge (ATB):** rhythm would depend on the combatant rather than on the cards, weakening
  "the spell line is the build".
- **Tag combos (shared tag with the previous card gives a bonus):** needs a tag catalogue, which is still an
  open question; could be added later on top of card-carried modifiers.
- **Health only:** enough to test the loop but leaves no defensive choice. **Adding a power buff:** overlaps
  with neighbour modifiers in the first prototype.
- **Enemy with a fixed attack rate:** simpler at first, but would require a second system for professors.
