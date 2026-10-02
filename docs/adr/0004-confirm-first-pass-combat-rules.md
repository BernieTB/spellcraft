# 0004. Confirm the first-pass combat details, targeting and neighbour modifier kinds

- **Status:** Accepted
- **Date:** 2026-10-02

## Context

[ADR 0002](0002-first-pass-combat-rules.md) recorded the owner's first-pass combat rules, plus a list of
"Provisional details" that Claude had filled in so the simulator could be built. ADR 0002 is Accepted and is
not rewritten, so the owner's answer is recorded here.

The combat loop (#12) also needed a targeting rule that no document described, and #13 needs to know which
neighbour modifiers exist in the prototype. ADR 0003 (card effect representation) was left Proposed until the
owner reviewed it.

The owner decided all of the points below on 2026-10-02. Like ADR 0002, they are first-pass rules for the
prototype and are expected to change after playtests (each change needs a new ADR).

## Decision

**ADR 0002 provisional details are confirmed as written.** In particular:

- casts completing on the same tick resolve hero first, then enemies in their order of appearance;
- a dead combatant stops casting, and the fight ends when the hero is dead or all enemies are dead;
- a neighbour modifier applies to one cast of the neighbour, then is used up, and several modifiers on the
  same cast add up;
- in a single-card line, the card's next and previous neighbour is itself;
- healing is capped at maximum health; shield has no cap and does not decay.

**Targeting.** The hero targets the first living enemy, in the enemies' order of appearance. Enemies target
the hero. Heal and gain-shield effects apply to their caster. Targeting chosen by card data remains possible
later, as ADR 0003 allows.

**Neighbour modifier kinds for the prototype.** A card may carry modifiers that give **+X damage**, **+X
heal** or **+X shield** to the **next** or the **previous** card in its own spell line. The direction is set
per modifier and X comes from the card data. A bonus only adds to effects of the same kind on the receiving
card.

**ADR 0003 is accepted.**

## Consequences

### Positive

- #12 and #13 build on decided rules; the code and docs no longer need to call them provisional.
- Three bonus kinds are enough to test whether card placement matters, without multipliers to balance.

### Negative

- The same-tick tie-break still favours the hero; balancing must watch it.
- A bonus of a kind the receiving card lacks is wasted, which players must be able to see in the recap.

## Alternatives considered

- **Targeting the weakest enemy:** smarter, but makes the order of enemies matter less and is harder to read.
- **Damage bonus only:** the minimum, but biased toward attack.
- **Bonus plus multipliers (for example "the next card has double effect"):** more spectacular, but harder to
  balance in the first prototype.
