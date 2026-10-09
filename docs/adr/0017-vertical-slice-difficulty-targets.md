# 0017. Difficulty targets and balance bots of the Vertical slice

- **Status:** Proposed
- **Date:** 2026-10-09

## Context

The first playtest of the Vertical slice (owner, 2026-10-09, #125) found the run far too easy: the line never had to be
rearranged. The numbers of the biome, the MVP class and the passive pool were first-pass values (ADR 0007, 0009, 0010,
0011), and every simulated run of the previous data won (100 % for every kind of player).

Two facts of the rules shape what can be tuned (ADR 0002, 0005, 0009):

- The hero's health does not carry over: every fight starts at full health, so the difficulty of a run is the
  difficulty of each fight. A fight is deterministic once the line and the encounter are fixed; the only randomness of a
  run is the encounter draws and the level-up offers.
- Neighbour bonuses are flat and added, and shield does not decay. Reordering only matters when a bonus is wasted (a
  bonus given to a card that has no effect of that kind), when a fight is tight enough for the timing of the casts to
  decide it, or when the player changes the cards of the line.

## Decision

We will tune the difficulty with data only (the specs read by the content generators), against bots that play complete
runs of the biome (`Tests/Unity/Balance`, on the real `Run`), and keep the result under test.

**Bots** (all use the same steps: a mini-boss room once enough regular fights are won, then the professor):

- **Naive:** never touches the line, always takes the first level-up package.
- **Intermediate:** orders the line and picks the packages and cards by the bonus arithmetic (damage per tick times
  health, shield and healing counted as health over a reference fight), with no knowledge of the coming fight.
- **Expert:** simulates the coming fight (a mini-boss as shown by its preparation, the professor as the mini-bosses'
  revelations show him, with the unrevealed card guessed) and hill-climbs the order and the cards of the line, and the
  level-up choices, on the simulated result. It is an upper bound of a human player.

**Targets** (proposed, to be confirmed by the owner), on the full biome (both rooms, then the professor):

| | Win rate |
|---|---|
| Naive | at most 25 % (the data gives about 0 %) |
| Intermediate | 40 to 70 % (about 50-55 %) |
| Expert | 75 to 95 % (about 85-90 %) |

Regular fights stay safe for an ordered line (at most 5 % of the runs of the intermediate and expert bots end in one)
and kill some runs of a disordered one; the mini-bosses are the main spike for a player who never touches the line; the
professor is a wall without the rooms' rewards and revelations (at most 30 % wins on the short path, at most half of the
full biome's rate); a short-path run lasts about 6 to 7 minutes at normal speed by the estimate of the harness, about
35 seconds per fight with the screens, as ADR 0009 counted.

**Data changes that follow** (all in the generator specs):

- The starting line is `CARD_A, CARD_B, CARD_C, CARD_D`: the weaver `CARD_C` boosts the shield card, so its bonus is
  wasted until the player moves it before a damage card.
- Cast times of every card are four times longer (a fight lasts tens of seconds at 4 ticks per second, a card takes
  1 to 5 seconds to cast), neighbour bonuses are doubled, so a well-placed bonus is worth a card and a misplaced one is
  a loss. Hero health is 40.
- Enemies have several times more health; the professor and the mini-bosses have worked decks and much more health and
  shield. The fight time limit is 2000 ticks (the longest fight of the skilled bots is about 1500).

## Consequences

### Positive

- The slice has a measurable difficulty that a data change cannot silently break: `BalanceTargetsTests` plays the bots on
  fixed seeds.
- The owner can change a target by editing a range and the specs, and read the effect in a table.

### Negative

- The bots are models, not people: the gap between the intermediate and the expert bots comes from the expert's exact
  simulation of the coming fight, so a human lies between them. The table of the PR is the thing to confirm, not a promise.
- A player who never touches the line cannot win: that is the intent, but the owner may want a softer floor.
- About one run in six of the naive bot ends in the first regular fights (a regular monster the starting line, as
  ordered, cannot beat).

## Alternatives considered

- **Tuning by hand from playtests only:** slow and not repeatable; the bots show the effect of each number in seconds.
- **Raising only the enemies' numbers:** a fight is deterministic, so a harder enemy made the naive and the ordered
  players fail together; making the order and the bonuses matter (doubled bonuses, disordered start, decks of the
  bosses) is what separates them.
