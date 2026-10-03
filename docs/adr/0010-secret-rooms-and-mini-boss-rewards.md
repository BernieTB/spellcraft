# 0010. Secret rooms, objectives and mini-boss rewards for the Vertical slice

- **Status:** Accepted
- **Date:** 2026-10-03

## Context

The GDD says secret rooms are unlocked by objectives, entered by clicking, and hide mythical mini-bosses that
give information about the professor. It did not say which objectives exist, how many rooms a biome has, what a
mini-boss gives or what happens on defeat (spike #68). [ADR 0009](0009-vertical-slice-run-pacing.md) already
settled part of it: any defeat ends the run, the first victory over each mini-boss gives +1 spell line slot and
more XP, a mini-boss can be fought again for XP only, and each mini-boss has unique loot to be defined here.

The owner decided the points below on 2026-10-03 from options compared during the spike. Numbers, monster
choices and the revealed cards live in data.

## Decision

**Objective type: defeat N monsters of kind X.** Each secret room has one objective of the form "defeat N of
monster X" (N and X from data). Progress counts victories over regular fights in the biome. Since regular fights
are drawn with the run's seed and the player may keep farming once the professor is available, an objective can
always be completed by fighting on.

**Two secret rooms per biome** in the Vertical slice, each with its own objective and its own mini-boss. With
the starting capacity of 4, the spell line can reach 6 slots.

**Rewards of the first victory over a mini-boss:**

- **+1 spell line slot** (ADR 0009);
- its **unique card**: a card found nowhere else, tied to the creature, which goes to the spell line or the
  reserve like a level-up card;
- **information about the professor**: some cards of the professor's spell line are **revealed**, and are shown
  during the preparation phase. Which cards each mini-boss reveals is set in data;
- more XP than a regular fight (ADR 0009).

Fighting a mini-boss again gives XP only: the slot, the unique card and the revealed cards are given once.

**Defeat in a secret room ends the run** (ADR 0009, strict roguelike).

## Consequences

### Positive

- One objective kind is easy to read, to track from fight results and to test.
- Secret rooms feed the three things the player cares about before the boss: line size, a new card and
  knowledge of the professor.
- Revealing professor cards turns the preparation phase into an informed decision, in line with "preparation
  over luck".

### Negative

- Each mini-boss needs a worked spell line and a unique card, and the professor's line must be readable once
  partly revealed: four pieces of authored content for the slice.
- An objective on a rare monster can take long to complete; the encounter pool must make every objective
  monster common enough (content, #72).
- Whether revealed information is kept across runs (bestiary) is not decided here.

## Alternatives considered

- **An objective plus a fight challenge (win under T ticks, without losing health):** more strategic, but more
  rules to build and explain in the slice.
- **A count of fights won:** opens the room without any player decision.
- **One or three secret rooms:** one barely tests the mechanic; three means more authored content and a longer
  biome.
- **A unique passive, or a choice between card and passive:** passives are not designed yet (#74).
- **Revealing the professor's stats or a text hint:** stats help little to arrange the line; a hint is vague
  and must be written by hand.
