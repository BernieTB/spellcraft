# Glossary

Key terms of Spellcraft. Mechanics are described in [`GAME_DESIGN.md`](GAME_DESIGN.md); this file only defines
words. Use these terms consistently in code, docs and issues.

| Term | Definition |
|---|---|
| **Bestiary** | Permanent record of knowledge about enemies, kept across runs. One of the sources of information during the preparation phase. See [Meta-progression](GAME_DESIGN.md#meta-progression). |
| **Biome** | One section of a run, representing one year at the magic school. Contains regular monsters, secret rooms and a professor. See [Run structure](GAME_DESIGN.md#run-structure). |
| **Card** | One element of the spell line. Each card is a word. Cards react to their neighbours and evolve through use. See [Spell line](GAME_DESIGN.md#spell-line). |
| **Cast time** | The number of ticks a card takes to cast. Its effects resolve when the cast ends. See [ADR 0002](adr/0002-first-pass-combat-rules.md). |
| **Combatant** | A participant in a fight: the player character or an enemy. Holds the combat stats: health and shield. Starting values come from data. In code: `Game.Core.Combat.Combatant`. |
| **Dead** | State of a combatant whose current health has reached zero. A dead combatant takes no damage, cannot be healed and gains no shield (placeholder rule: no revival mechanic is designed). |
| **Evolution** | A card growing stronger the more it is cast, for example by gaining letters up to a 5-letter maximum. |
| **Grimoire** | Meta-progression collection of discovered named spells. |
| **Health** | Combat stat. **Max health** is the upper bound, set by data; **current health** is what remains, between zero and max health. Damage not absorbed by shield lowers current health (never below zero); healing raises it (never above max health). |
| **Linked choice** | The level-up reward: a package of one passive upgrade plus one card. The player picks a package, not the two parts separately. See [Character and level-ups](GAME_DESIGN.md#character-and-level-ups). |
| **Meta-progression** | Progress kept between runs. It widens options (cards in the pool, classes, bestiary, grimoire), not raw stat power. |
| **Mini-boss** | A mythical creature found in a secret room. |
| **Neighbour modifier** | A bonus a card grants, each time it resolves, to the next or the previous card in its own spell line (direction set per modifier). Kinds: **+X damage**, **+X heal** or **+X shield**, X from data. It applies to one cast of that neighbour, then is used up; several bonuses on the same cast add up. It adds only to an effect of the same kind, once per cast: a +damage bonus on a card with no damage effect does nothing. The line loops (the next card after the last is the first), a card alone in its line is its own neighbour, and a "previous" bonus applies on the next loop. In code: `Game.Core.Cards.NeighbourModifier`. See [ADR 0002](adr/0002-first-pass-combat-rules.md), [ADR 0004](adr/0004-confirm-first-pass-combat-rules.md) and [ADR 0005](adr/0005-neighbour-modifier-resolution.md). |
| **Passive upgrade** | The non-card half of a linked choice: an improvement to the character. Its catalogue is not designed yet. |
| **Preparation phase** | The step before a professor fight where the player studies the boss, rearranges the spell line and may swap cards from a limited reserve. See [Boss preparation phase and recap](GAME_DESIGN.md#boss-preparation-phase-and-recap). |
| **Professor** | The boss of a biome. |
| **Recap** | The detailed report after a boss fight explaining the result (e.g. damage per card, where the chain broke). |
| **Run** | One attempt, from start to defeat or victory, lasting 20 to 30 minutes across several biomes. Losing ends the run. |
| **Secret room** | A room unlocked by completing an objective (e.g. defeat N of monster X), entered by clicking. Hides a mini-boss. |
| **Shield** | Combat stat that absorbs damage before health: damage first reduces shield, and only the remainder reduces health. Starts at a value from data (can be zero) and can be gained during a fight. Healing does not restore it. No maximum. |
| **Spell line** | The player's build: an ordered sequence of cards that plays automatically in a loop. Position matters. |
| **Tick** | The discrete unit of time in a fight. The simulation advances tick by tick, never by real time. |
| **Word** | The text of a card: 2 to 5 letters, ancient-sounding, mostly meaningless (Latin-like). Final words are written by the project owner. |
