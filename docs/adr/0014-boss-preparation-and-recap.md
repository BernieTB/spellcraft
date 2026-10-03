# 0014. Boss preparation information, reserve and recap content

- **Status:** Accepted
- **Date:** 2026-10-04

## Context

The GDD describes a preparation phase where the player studies the boss with partial information and swaps cards
from a "limited reserve", and a recap that explains the result, "such as damage per card and where the chain
broke" (spike #81). Earlier decisions already settled part of it:

- the preparation phase happens before every mini-boss and professor fight, and the line is fixed once those
  fights start ([ADR 0012](0012-linked-choices-and-spell-line-editing.md));
- any defeat ends the run, so a preparation cannot be retried ([ADR 0009](0009-vertical-slice-run-pacing.md));
- the first victory over a mini-boss reveals information about the professor
  ([ADR 0010](0010-secret-rooms-and-mini-boss-rewards.md));
- the combat log records every cast, effect, bonus and wasted bonus.

The owner decided the points below on 2026-10-04 from options compared during the spike.

## Decision

**Reserve: no size limit.** Every card the player owns outside the spell line is in the reserve (cards gained
while the line is full, replaced cards, unique mini-boss cards). Nothing is ever discarded; the line capacity is
the only limit. The GDD's "limited reserve" is dropped.

**Professor information: nothing but revelations.**

- By default the professor's health, shield and cards are all **hidden** during the preparation.
- Each mini-boss's data lists what its first defeat reveals: some of the professor's **cards**, and/or their
  **health** or **shield**.
- Revealed information is **kept permanently** in the bestiary: it stays known in later runs. This is
  meta-progression as knowledge, not power.

**Recap content.** The recap is shown after every mini-boss and professor fight and after any defeat. It is built
only from the combat log and shows:

- **per card**, for the hero's and the enemy's lines: damage, healing and shield produced, and number of casts;
- **neighbour bonuses**: how many were used and how many were wasted, and why (the receiving card had no effect
  of that kind).

**Where the chain broke (on a defeat).** The recap finds the **turning point**: the tick after which the hero's
health stays below the enemy's until the end. It then highlights the most visible **cause** around that point,
from the log: wasted bonuses, the weakest card of the line, or the shield being broken. This diagnosis is
computed by rules in Core, so the same fight always gives the same explanation.

## Consequences

### Positive

- An unlimited reserve keeps every reward meaningful and makes the preparation a pure placement decision.
- Hidden information makes mini-bosses and the bestiary the real way to prepare, and permanent knowledge gives
  later runs an edge without raw power.
- A recap built from the log alone is deterministic, testable and always matches what happened.

### Negative

- A first professor fight is played almost blind unless the player beats the mini-bosses first.
- The bestiary becomes save data that must persist across runs, earlier than the rest of meta-progression.
- The turning point and cause rules are heuristics: they need tuning on real fights and may point at the wrong
  cause in close fights.
- What the player knows about a mini-boss before its own fight is not decided yet.

## Alternatives considered

- **Reserve with a size limit:** harder choices, but rewards could be lost.
- **Professor stats always visible, or revelations kept for one run only:** more help, or no long-term
  knowledge; the owner chose hidden stats and a permanent bestiary.
- **Health curve and evolution events in the recap:** not kept for the first version; they can be added later
  from the same log.
- **Weakest card only, or no diagnosis:** simpler, but does not answer "when" or leaves the player alone with
  the numbers.
