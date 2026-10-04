# 0015. Live spell line editing details

- **Status:** Accepted
- **Date:** 2026-10-04

## Context

[ADR 0012](0012-linked-choices-and-spell-line-editing.md) lets the player edit the hero's spell line during
regular fights, with an immediate effect: the card being cast finishes its cast, the line continues from the card
now after its position, and pending neighbour bonuses stay at their position. Building it (#97) raised details the
ADR does not settle: what happens when the card being cast is itself moved or sent to the reserve, the order of
several changes on one tick, and when a cast takes its pending bonus.

[ADR 0005](0005-neighbour-modifier-resolution.md) says a card takes and clears its pending bonus when it
resolves. With live editing, the card that resolves may no longer sit at the position it started from, so "when it
resolves" no longer names one position.

The owner confirmed the rules below on 2026-10-04.

## Decision

**A cast takes its card and its pending bonus when it starts** (its first tick), not when it resolves. This
refines ADR 0005. Without line changes it gives the same fights: only the combatant's own resolutions grant
bonuses to its positions, one cast at a time, so nothing can reach a position between the start and the end of a
cast. All existing golden fights are unchanged.

**The card being cast** keeps casting whatever happens to the line, and resolves with the card and bonus it
started with. Where it resolves, and so where the line continues from:

- **moved** (it moves, or another card moves across it): it resolves at its new position, and the line continues
  after that position. While the card is in the line, its anchor follows it;
- **sent to the reserve**: it resolves at the position it left, where the incoming card now sits, and the line
  continues after that position. From then on the anchor is fixed on that slot, even if the incoming card is
  moved later in the same cast.

Its neighbour modifiers aim at the neighbours of that position, in the line as it is when it resolves.

**Several changes on one tick** apply in the order given. **Moving a card onto its own position** is ignored: no
record, no log event.

**API.** A `LineChange` is stamped with the tick it applies to and applies at the start of that tick, before any
combatant acts. A fight is created with the hero's reserve and an edit flag: true for regular fights, false for
mini-boss and professor fights, which refuse every change. It can be driven two ways that give the same fight:

- `Run(lineChanges)`: a schedule in tick order, validated before the first tick (headless simulation, tests,
  replays);
- `ApplyLineChange(change)` then `Step()`, one tick at a time, for the run screen.

Applied changes are returned in `FightResult.LineChanges` and recorded as `line` events in the combat log. The run
replays them on its own spell line and reserve after the fight.

## Consequences

### Positive

- Every edit has one predictable outcome, which the combat log and recap can explain.
- The same fight code serves the headless simulator and a screen that drives the fight tick by tick.
- Fights without changes, and the simulator, behave exactly as before.

### Negative

- ADR 0005's wording no longer matches the code; this ADR is the reference for when a bonus is taken.
- A bonus taken by a card that is then sent to the reserve leaves with it, which the player may not expect; the
  run screen must make the card being cast visible.
- The run must replay the changes on its card instances after each regular fight (follow-up ticket).

## Alternatives considered

- **Take the bonus on resolution, as ADR 0005 says:** a card moved during its cast would take the bonus of its
  new slot, so a player could move a card onto a boosted slot at the last tick; harder to read.
- **Anchor that keeps following the slot's card after a swap:** moving the incoming card would move the point the
  line continues from, although the card being cast is no longer in the line.
- **Refuse a no-op move:** it adds an error the screen must handle for a harmless input.
