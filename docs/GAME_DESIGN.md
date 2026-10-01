# Spellcraft: Game Design Document

> **Status:** living document, last updated 2026-10-01.
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
2. On each level-up, the player takes a [linked choice](#character-and-level-ups) and grows the spell line.
3. Objectives unlock [secret rooms](#mini-bosses-and-secret-rooms) with mini-bosses.
4. Before the biome's professor, the player [prepares](#boss-preparation-phase-and-recap), then watches the fight.
5. Win: move to the next biome. Lose: the run ends.

## Run structure

- A run lasts **20 to 30 minutes**. There is no offline progression.
- A run is made of several **biomes**. Each biome is one year at a magic school.
- Per biome: auto-fights against regular monsters (XP), secret rooms with mini-bosses, and a **professor** as the boss.
- **Strict roguelike:** losing ends the run.

## Character and level-ups

- The player controls **one character** of a chosen **class**.
- Each level-up offers a **linked choice**: one passive upgrade paired with one card to add to the spell line.
  The player picks a package, not two independent items.

## Spell line

- The spell line is an **ordered sequence of cards** that plays automatically, **in a loop**.
- **Cards are words.** Words are 2 to 5 letters, ancient-sounding and mostly meaningless (Latin-like).
  Final words are written by the project owner.
- **Neighbour effects:** cards react to their neighbours, so position matters. Exact rules are an open question.
- **Evolution through use:** a card cast many times grows stronger, for example by gaining letters, up to a
  5-letter maximum.

## Combat

- Combat is **automatic**. The player does not act during a fight.
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

- Secret rooms are unlocked by **objectives** (e.g. defeat N of monster X). The player clicks to enter.
- Secret rooms hide **mini-bosses**, which are **mythical creatures**.
- Mini-bosses and secret rooms are a source of information about the biome's professor.

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

- Class identity and starting deck(s) for the MVP.
- Element/tag catalogue and exact neighbour-effect rules, and whether grammar-like roles are used at all.
- Whether elements are represented as ink colours, and whether word sounds map to elements.
- Exact level-up pacing, deck size, spell line length per biome, number of biomes.
- Number and design of professors and mini-bosses.
- Audio direction, final title (working title: Spellcraft), art pipeline.
