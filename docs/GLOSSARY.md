# Glossary

Key terms of Spellcraft. Mechanics are described in [`GAME_DESIGN.md`](GAME_DESIGN.md); this file only defines
words. Use these terms consistently in code, docs and issues.

| Term | Definition |
|---|---|
| **Bestiary** | Permanent record of knowledge about enemies, kept across runs. One of the sources of information during the preparation phase. See [Meta-progression](GAME_DESIGN.md#meta-progression). |
| **Biome** | One section of a run, representing one year at the magic school. Contains regular monsters, secret rooms and a professor. See [Run structure](GAME_DESIGN.md#run-structure). |
| **Card** | One element of the spell line. Each card is a word. Cards react to their neighbours and evolve through use. See [Spell line](GAME_DESIGN.md#spell-line). |
| **Cast time** | The number of ticks a card takes to cast. Its effects resolve when the cast ends. See [ADR 0002](adr/0002-first-pass-combat-rules.md). |
| **Evolution** | A card growing stronger the more it is cast, for example by gaining letters up to a 5-letter maximum. |
| **Grimoire** | Meta-progression collection of discovered named spells. |
| **Linked choice** | The level-up reward: a package of one passive upgrade plus one card. The player picks a package, not the two parts separately. See [Character and level-ups](GAME_DESIGN.md#character-and-level-ups). |
| **Meta-progression** | Progress kept between runs. It widens options (cards in the pool, classes, bestiary, grimoire), not raw stat power. |
| **Mini-boss** | A mythical creature found in a secret room. |
| **Neighbour modifier** | A bonus carried by a card that applies to the next or previous card in the spell line (e.g. "the next card deals +X damage"). See [ADR 0002](adr/0002-first-pass-combat-rules.md). |
| **Passive upgrade** | The non-card half of a linked choice: an improvement to the character. Its catalogue is not designed yet. |
| **Preparation phase** | The step before a professor fight where the player studies the boss, rearranges the spell line and may swap cards from a limited reserve. See [Boss preparation phase and recap](GAME_DESIGN.md#boss-preparation-phase-and-recap). |
| **Professor** | The boss of a biome. |
| **Recap** | The detailed report after a boss fight explaining the result (e.g. damage per card, where the chain broke). |
| **Run** | One attempt, from start to defeat or victory, lasting 20 to 30 minutes across several biomes. Losing ends the run. |
| **Secret room** | A room unlocked by completing an objective (e.g. defeat N of monster X), entered by clicking. Hides a mini-boss. |
| **Spell line** | The player's build: an ordered sequence of cards that plays automatically in a loop. Position matters. |
| **Tick** | The discrete unit of time in a fight. The simulation advances tick by tick, never by real time. |
| **Word** | The text of a card: 2 to 5 letters, ancient-sounding, mostly meaningless (Latin-like). Final words are written by the project owner. |
