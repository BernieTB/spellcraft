# 0007. MVP class identity and starting deck

- **Status:** Accepted
- **Date:** 2026-10-02

## Context

The MVP has one playable class (GDD, "MVP scope"), and its identity and starting deck were an open question.
Every system of the Vertical slice (run, level-ups, evolution, boss preparation) is tested with this class, so
it must be settled first (spike #64). Final names and card words are written by the owner, never by Claude
Code. The owner decided the points below on 2026-10-02 from options compared during the spike.

## Decision

**Play style: combo weaver.** The MVP class wins through card placement. Its cards are modest on their own and
become strong through neighbour modifiers ([ADR 0002](0002-first-pass-combat-rules.md),
[ADR 0004](0004-confirm-first-pass-combat-rules.md)), which puts the core mechanic ("the spell line is the
build") at the centre of the first class. The class name is not decided; it is referred to as `CLASS_A`.

**Starting deck: 4 cards**, which is also the starting spell line. Each level-up then adds a card that matters.

**Effect mix: mostly damage plus one defensive card.**

- two damage cards;
- one weaver card that gives a neighbour modifier (a bonus to the next or previous card);
- one defensive card.

**Card words come later.** Until the owner writes them, the cards use obvious placeholders (`CARD_A` to
`CARD_D`). Replacing them is a data change only.

**Left to later decisions:**

- whether the defensive card gives shield or healing depends on whether health carries over between fights
  (run pacing, #67);
- base health, base shield, card amounts, cast times and modifier values are balanced with the simulation
  runner once the pacing is decided; they live in data.

## Consequences

### Positive

- The first class showcases what makes the game different, so playtests test the right thing.
- A 4-card line stays readable on screen and in the recap.
- Numbers can be tuned with the simulator without touching code or this decision.

### Negative

- A combo class is harder to read for a new player than a balanced one; the run screen and recap must make
  bonuses visible (the combat log already records them).
- Until the words exist, playtests show placeholder names.

## Alternatives considered

- **Balanced apprentice:** easier to learn, but no distinctive identity and placement matters less.
- **Glass cannon (offensive):** readable, but shield and healing lose their point.
- **Guardian (defensive):** long, safe fights that are less interesting to watch.
- **3 cards:** too few combinations at the start. **5 or 6 cards:** each new card weighs less.
- **One card of each effect:** shows every mechanic at once but dilutes the class identity.
