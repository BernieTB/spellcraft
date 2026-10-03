# 0012. Level-up linked choices, passive upgrades and spell line editing

- **Status:** Accepted
- **Date:** 2026-10-04

## Context

The GDD says each level-up offers a linked choice: one passive upgrade paired with one card, picked as a
package. It did not say which passive upgrades exist, how many packages are offered and how they are drawn,
where the new card goes, or when the player may change the spell line (spike #74). What happens when the line
is full is already decided: the card goes to the reserve or replaces a line card
([ADR 0009](0009-vertical-slice-run-pacing.md)).

So far the player never acted during a fight (GDD, "Combat"). The owner's answers change that for regular
fights, which [ADR 0011](0011-enemy-tiers-and-fight-time-limit.md) made the idle part of the game.

The owner decided the points below on 2026-10-03 and 2026-10-04 from options compared during the spike. All
values live in data.

## Decision

**First catalogue of passive upgrades.** A passive upgrade lasts for the rest of the run. Three kinds:

- **Base stats:** +X max health, or +X starting shield at the start of every fight.
- **Effect kind bonus:** +X to every effect of one kind (damage, heal or shield) the hero casts.
- **Stronger neighbour bonuses:** +X to the neighbour bonuses the hero's cards give (fits the combo weaver,
  [ADR 0007](0007-mvp-class-and-starting-deck.md)).

The same passive taken several times **stacks** (two "+X damage" give +2X). Faster casting is not in the first
catalogue.

**Offers.** Each level-up offers **3 packages**. Each package pairs one card drawn from the class's card pool
with one passive drawn from the passive pool, with the run's seed, so the same seed and choices give the same
offers. There is no rarity in the Vertical slice. A card the player already owns can be offered again:
**duplicates are allowed**.

**Where the new card goes.** The chosen card is **added at the end** of the spell line when a slot is free;
otherwise it goes to the reserve or replaces a line card (ADR 0009).

**Editing the spell line.**

- **During regular fights**, the player may rearrange the line and swap cards with the reserve at any time,
  including while a fight plays. A change applies **immediately**:
  - the card being cast **finishes its cast** and its effects resolve;
  - the line then continues from the card now placed after the position of that card;
  - neighbour bonuses waiting to be used **stay at their position** in the line: the card that now sits in
    that slot receives them.
- **Before a mini-boss or a professor fight**, the player has a **preparation phase** to rearrange the line and
  swap cards with the reserve. Once these fights start, the line is **fixed**: the player only watches.

**Determinism.** A line change during a fight is an input of the fight, stamped with its tick. The same seed and
the same inputs give the same fight, and the combat log records each change so the fight stays explainable.

## Consequences

### Positive

- Regular fights stay idle but not passive: the player can test placements live and see the effect at once.
- Mini-boss and professor fights keep the "preparation over luck" pillar: everything is decided before the fight.
- Two independent pools need little authored content and give many combinations; stacking passives lets the
  player specialise.

### Negative

- The Core fight loop must accept line changes between ticks, and the simulator, the combat log and the recap
  must handle them; this is new work for #69, #73 and #76.
- Bonuses that stay at their position can be moved onto a card of another kind and be wasted; the screen must
  make pending bonuses visible.
- Random pairing can produce packages whose two halves do not fit together.
- Duplicates and stacking can make one strategy dominate; balancing with the simulator must watch it.

## Alternatives considered

- **Faster casting as a passive:** strong and hard to balance (cast time has a 1-tick minimum).
- **2 packages, or 3 with a reroll:** two is often an obvious choice; a reroll is one more rule.
- **Hand-written packages or rarity tiers:** more coherent or more surprising, but much more content and tuning.
- **Player chooses the position of the new card:** more decisions at each level-up, redundant once the line can
  be edited freely.
- **Changes applied at the next fight, or only between fights:** simpler to simulate, but the owner wants the
  idle fights to react at once.
- **Editing during mini-boss and professor fights:** would weaken the preparation phase.
- **Cancelling the current cast, or applying changes at the end of the loop:** punishing or less immediate.
- **Bonuses following their card, or lost on any change:** the owner chose bonuses attached to the slot.
