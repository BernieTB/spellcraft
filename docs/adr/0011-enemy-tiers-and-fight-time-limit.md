# 0011. Enemy tiers and the fight time limit

- **Status:** Accepted
- **Date:** 2026-10-03

## Context

The Core fight loop stops after a maximum number of ticks so that a fight no side can win (for example two
sides that only gain shield) cannot run forever, in the game or in the simulation runner. The limit is passed by
the caller (`Fight`, `maxTicks`) and a fight that reaches it ends with no winner (`FightResult.TimedOut`). No
document said where the limit lives or what a timeout means for the player (question raised in #66).

The owner also described on 2026-10-03 how enemies differ between regular fights and boss fights, which the GDD
did not say and which the content work (#72) and the timeout rule depend on.

## Decision

**Two tiers of fights.**

- **Regular fights are the idle part of the game.** Regular monsters are very simple: their spell line holds a
  **single attack card**. The player watches the character grow.
- **Mini-bosses and professors are the strategic part.** They have **worked spell lines** of several cards,
  designed so that the player's preparation matters.

**Fight time limit: a safety net.** Endless fights are unlikely with these tiers, so the limit is only a guard:

- one **global value in data**, far above the length of a normal fight, so it is never reached in normal play;
- a fight that reaches it counts as a **defeat** of the hero (so, like any defeat, it ends the run), and the
  recap says the fight ran out of time;
- the simulation runner keeps counting timeouts separately, so a boss line that can cause one is caught while
  balancing and fixed in its data.

## Consequences

### Positive

- Regular content is cheap to author (one card per monster) and fast to read on screen.
- Authoring effort goes where decisions happen: mini-boss and professor lines.
- Every fight ends, with an outcome the player can understand.

### Negative

- A hero with a fully defensive line could lose a boss fight on time; the recap must make that clear.
- The run model (#69) has to read the limit from a global data asset instead of taking it from each caller.

## Alternatives considered

- **Limit per encounter:** more control, but one more number on every encounter for a case that should not
  happen.
- **Sudden death (damage rising over time):** always ends the fight, but adds a rule to build and balance for a
  rare case.
- **Timeout as a draw (no reward, no defeat):** lenient, but could be exploited by a defensive line.
