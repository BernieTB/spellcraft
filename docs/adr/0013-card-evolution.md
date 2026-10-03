# 0013. Card evolution through use

- **Status:** Accepted
- **Date:** 2026-10-04

## Context

The GDD said a card cast many times grows stronger, "for example by gaining letters, up to a 5-letter maximum",
without saying how many stages there are, what changes, how long it lasts or how it is shown (spike #78). Issue
#79 already states that evolution belongs to a card instance in the line, not to the card definition.

The owner decided the points below on 2026-10-04 from options compared during the spike. Thresholds and stage
values live in the card data.

## Decision

**Two evolution stages per card.** A card starts at its base form and can evolve twice. The number of casts
needed for each stage is set in the card's data.

**The word does not change.** Evolution makes a card stronger without adding letters; the GDD's "gaining
letters" example is dropped.

**What changes.** Each stage is defined in the card's data and can improve **anything except the cast time**:
effect amounts (damage, heal, shield), the card's neighbour modifiers, and the effects or modifiers the card
has. The cast time stays the same at every stage.

**Counting and duration.**

- Each card instance counts its own casts over the **whole run**, across fights. Two copies of the same card
  evolve separately.
- A card moved to the reserve **keeps** its stage and its count.
- Everything resets at the next run: evolution is not meta-progression.

**Display.** An evolved card shows a **stage mark** on screen (for example one or two marks), with a short
visual cue when it evolves. The evolution is an event in the combat log and the recap. There is no progress
gauge: the player does not see how many casts remain before the next stage.

## Consequences

### Positive

- Words stay as the owner writes them, with no extra forms to write per card.
- Cast time never changes, so a card's rhythm in the line stays predictable while its strength grows.
- Keeping the stage in the reserve makes swapping cards a free decision, not a loss.

### Negative

- Every card needs two stages of data; content authoring and balancing double for each card.
- Without a gauge, the player cannot plan around the next evolution; the stage mark and the log must make each
  evolution noticeable.
- Live line editing ([ADR 0012](0012-linked-choices-and-spell-line-editing.md)) and evolution both act on card
  instances: the run model must track instances, not definitions (#69, #79).

## Alternatives considered

- **One letter per stage up to 5 letters:** ties evolution to the word, but needs evolved words written for
  every card and makes the number of stages depend on word length.
- **One stage only:** too little growth over a run.
- **Values only, or values and cast time:** the owner wants modifiers and effects to evolve too, and cast time
  never to change.
- **Reset when moved to the reserve, or per fight:** makes swapping costly or changes the rhythm of the game.
- **Progress gauge:** helps planning but adds screen clutter; the owner prefers a stage mark only.
