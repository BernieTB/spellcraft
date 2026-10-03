# Spellcraft: Game Design Document

> **Status:** living document, last updated 2026-10-03.
> Only decided design goes in the body. Anything undecided goes in [Open questions](#open-questions).
> Terms are defined in [`GLOSSARY.md`](GLOSSARY.md).

## Vision and pillars

A roguelike auto-battler about building an incantation. The player composes a **spell line** of ancient-sounding
words, then watches a single character fight with it. Inspired by idle RPGs (watch the character fight and grow)
and by Hades-style run structure.

Pillars:

1. **The spell line is the build.** Order, neighbours and evolution of cards are where decisions happen.
2. **Preparation over luck.** Combat minimises randomness, so a well-prepared spell line wins.
3. **Fair, readable outcomes.** Every fight can be explained after the fact.
4. **Serious magic, funny world.** The magic is mysterious; the humour lives in names.

## Core loop

1. The character auto-fights regular monsters in a biome and earns XP.
2. On each level-up, the player takes a [linked choice](#character-and-level-ups) and gains a card.
3. Objectives unlock [secret rooms](#mini-bosses-and-secret-rooms) with mini-bosses, which widen the spell line.
4. Once the professor is available, the player chooses when to face them: [prepare](#boss-preparation-phase-and-recap),
   then watch the fight.
5. Win: move to the next biome. Lose any fight: the run ends.

## Run structure

- A run lasts **20 to 30 minutes**. There is no offline progression.
- A run is made of several **biomes**. Each biome is one year at a magic school.
- Per biome: auto-fights against regular monsters (XP), secret rooms with mini-bosses, and a **professor** as the boss.
- **Strict roguelike:** losing any fight (regular monster, mini-boss or professor) ends the run.

Pacing (see [ADR 0009](adr/0009-vertical-slice-run-pacing.md); numbers live in data):

- A biome targets **about 8 minutes** when the player goes to the professor as soon as possible, so a run holds
  about three biomes.
- Between fights the player picks the next step: a regular fight, an unlocked secret room, or the professor.
- The professor becomes available after a **minimum number of regular fights** (about 8). The player may then
  keep fighting regular monsters and mini-bosses before facing the professor.
- Regular fights are drawn with the run's seed from the biome's pool of encounters.
- **Each fight starts fresh:** the hero has max health and the starting shield at the start of every fight.

## Character and level-ups

- The player controls **one character** of a chosen **class**.
- **MVP class** (name to be decided, `CLASS_A`): a **combo weaver**. Its cards are modest alone and strong
  through neighbour modifiers. It starts with **4 cards** (two damage cards, one card that boosts a neighbour,
  one defensive card); the words are written by the owner. Numbers live in data. See
  [ADR 0007](adr/0007-mvp-class-and-starting-deck.md).
- Each level-up offers a **linked choice**: one passive upgrade paired with one card to add to the spell line.
  The player picks a package, not two independent items.
- XP comes from defeated enemies (mini-bosses give more). Each level costs more XP than the previous one,
  tuned for **about 5 level-ups** per biome on the shortest path; farming brings further levels more and more
  slowly, with no hard cap. See [ADR 0009](adr/0009-vertical-slice-run-pacing.md).
- The MVP class's defensive card **gives shield** (health resets every fight).

## Spell line

- The spell line is an **ordered sequence of cards** that plays automatically, **in a loop**.
- **Capacity:** the spell line starts with **4 slots** and gains a slot the first time each mini-boss is
  defeated. A card gained while the line is full goes to the **reserve**, or replaces a line card, which then
  goes to the reserve. See [ADR 0009](adr/0009-vertical-slice-run-pacing.md).
- **Cards are words.** Words are 2 to 5 letters, ancient-sounding and mostly meaningless (Latin-like).
  Final words are written by the project owner.
- **Cast time:** each card takes a number of ticks to cast (from data). Its effects resolve when the cast
  ends, then the next card starts.
- **Neighbour effects:** cards react to their neighbours, so position matters. First pass: a card may carry
  modifiers giving +X damage, +X heal or +X shield to the next or previous card in the loop, used up after one
  cast. No tags or elements yet. See [ADR 0002](adr/0002-first-pass-combat-rules.md) and
  [ADR 0004](adr/0004-confirm-first-pass-combat-rules.md).
- **Evolution through use:** a card cast many times grows stronger, for example by gaining letters, up to a
  5-letter maximum.

## Combat

- Combat is **automatic**. The player does not act during a fight.
- Time advances in **ticks**. The hero and each enemy cast their own spell line in a loop; enemies use the
  same system as the hero.
- First-pass stats: **health** and **shield**. Shield absorbs damage before health. The first effects are deal
  damage, heal and gain shield. See [ADR 0002](adr/0002-first-pass-combat-rules.md).
- **Two tiers of fights.** Regular fights are the idle part: regular monsters are very simple, with a single
  attack card. Mini-bosses and professors are the strategic part, with worked spell lines of several cards.
  See [ADR 0011](adr/0011-enemy-tiers-and-fight-time-limit.md).
- **Time limit:** a fight that reaches a global maximum number of ticks (from data, far above a normal fight)
  counts as a defeat, and the recap says so. It is a safety net, not a mechanic.
- Targeting (first pass): the hero targets the first living enemy, enemies target the hero; heal and shield
  apply to the caster. See [ADR 0004](adr/0004-confirm-first-pass-combat-rules.md).
- Randomness is **minimised** so that preparation decides outcomes.
- Technical requirements: the simulation is **deterministic** (seeded RNG, no wall-clock time) and runs
  **headless**, so thousands of fights can be simulated for balancing. See
  [ADR 0001](adr/0001-separate-core-logic-from-unity.md).

## Boss preparation phase and recap

**Preparation phase** (before each professor fight):

- The player studies the boss. Information is **partial**, gathered through mini-bosses, secret rooms and the
  permanent bestiary.
- The player may **rearrange** the spell line and **swap cards** from a limited reserve.

**Combat:** once it starts, it is fully automatic. The player watches.

**Recap** (after the fight): a detailed explanation of the result, such as damage per card and where the chain
broke, so a defeat feels fair.

## Mini-bosses and secret rooms

- Secret rooms are unlocked by **objectives** of the form **defeat N of monster X** (from data), counted over
  regular fights. The player clicks to enter. See [ADR 0010](adr/0010-secret-rooms-and-mini-boss-rewards.md).
- The Vertical slice biome has **two secret rooms**, each with its own objective and mini-boss.
- Secret rooms hide **mini-bosses**, which are **mythical creatures**.
- The first victory over each mini-boss gives:
  - **+1 spell line slot**;
  - its **unique card**, found nowhere else, which goes to the spell line or the reserve;
  - **information about the professor**: some cards of the professor's spell line are revealed (from data) and
    shown during the preparation phase;
  - more XP than a regular fight.
- A mini-boss can be fought again for XP only. Losing in a secret room ends the run.

## Meta-progression

Meta-progression **widens options rather than raw power**:

- new cards in the pool;
- new classes;
- the **bestiary** (permanent knowledge of enemies);
- the **grimoire** of discovered named spells.

## Setting and tone

- Colourful and adventurous, with a humorous edge.
- The magic itself is mysterious and serious.
- Humour and pop-culture allusions live in the **names** of professors, items, places and some cards.
- Greek mythology is free to use.
- Protected works (Harry Potter, Naruto, etc.): **allusions only**. Never use their proper names, invented
  terms or recognisable visuals.
- The game must be fully enjoyable without catching any reference.

## Art direction

Warm 2D, flat colours, ink/pen look.

## MVP scope

- 1 biome
- 1 class
- About 10 cards
- 1 boss (professor)

## Open questions

Not decided. Do not implement a choice for these without the owner's decision.

- MVP class name and card words (owner); its base stats and card values (balanced with the simulator).
- Element/tag catalogue, neighbour rules beyond the first pass (ADR 0002), and whether grammar-like roles are
  used at all.
- Whether elements are represented as ink colours, and whether word sounds map to elements.
- Number of biomes in the full game; pacing of biomes after the first.
- Number of secret rooms in biomes after the first; whether revealed professor cards are kept across runs
  (bestiary).
- Design of professors and mini-bosses (creatures, spell lines, unique cards); their number in the full game.
- Audio direction, final title (working title: Spellcraft), art pipeline.
