# 0009. Run pacing for the Vertical slice biome

- **Status:** Accepted (amended by [ADR 0016](0016-regular-fights-chain-automatically.md): regular fights chain automatically)
- **Date:** 2026-10-03

## Context

The GDD left the run pacing open: how many fights come before the professor, how XP and levels progress,
whether the hero's health carries over between fights, how the spell line grows and how long one biome lasts
(spike #67). A run lasts 20 to 30 minutes over several biomes; the MVP has one biome. Every Vertical slice
feature of the run (#69 run model, #70 XP and level-ups, #71 objectives, #72 content, #73 run screen) builds on
these answers. [ADR 0007](0007-mvp-class-and-starting-deck.md) also left the MVP defensive card (shield or
healing) to this decision.

The owner decided the points below on 2026-10-03 from options compared during the spike. All numbers live in
data and are tuned with the simulation runner ([ADR 0006](0006-headless-simulation-runner.md)).

## Decision

**Target length: about 8 minutes per biome** for a player who goes to the professor as soon as possible, so a
20 to 30 minute run holds about three biomes.

**The player chooses when to face the professor.** Between fights the player picks the next step: a regular
fight, an unlocked secret room, or the professor once available.

- The professor becomes available after a **minimum number of regular fights** (from data, about **8** to fit
  the target length).
- After that, the player may keep fighting regular monsters and mini-bosses as long as they want before
  facing the professor (farming).
- Regular fights are drawn with the run's seed from the biome's pool of encounters, so runs differ while
  staying reproducible. How the pool grades encounters by difficulty is set in data.

**Any defeat ends the run** (strict roguelike), whether against a regular monster, a mini-boss or the
professor. Farming therefore carries a risk.

**Health and shield reset at the start of every fight.** The hero starts each fight at max health with the
starting shield from data. Healing only matters within the fight where it happens. As a consequence, the MVP
class's defensive card (ADR 0007) **gives shield**.

**XP and levels.**

- Each enemy gives XP from its data; mini-bosses give more than regular monsters.
- Each level costs more XP than the previous one (curve in data), tuned so that **about 5 level-ups** come
  naturally over the minimum path. Farming stays possible but brings levels more and more slowly; there is no
  hard level cap.
- Each level-up is a linked choice (GDD), so it adds one card.

**Spell line capacity grows through mini-boss rewards, not levels.**

- The line starts with a capacity of **4**, filled by the starting deck (ADR 0007).
- Defeating a mini-boss **for the first time** gives **+1 line slot**. A mini-boss can be fought again for
  XP, but its slot is given only once.
- Each mini-boss also has its own **unique loot**, whose nature is decided with the secret rooms (#68).
- A card gained when the line is full goes to the **reserve**. The player may instead put it in the line in
  place of a line card, which then goes to the reserve. The reserve is the pool used to swap cards during the
  boss preparation phase (#81 decides when else it can be used).

## Consequences

### Positive

- Each fight is independent, which keeps balancing with the simulator simple: one fight's result does not
  depend on the previous ones.
- Secret rooms and objectives carry a reward that shapes the build (line size), not only information.
- The player controls the risk/reward of farming, and the rising XP curve keeps farming from trivialising
  the professor.
- Level-ups keep a meaning when the line is full: the new card feeds the reserve for the boss preparation.

### Negative

- With health reset, there is no attrition across fights; tension comes only from the risk of losing a
  fight and from the professor.
- Healing cards are weaker than shield cards when fights are short; card values must account for it.
- The run needs a "choose the next step" screen, which #73 must cover.
- An 8-minute biome with about 8 regular fights, a few mini-bosses and the professor leaves roughly 30 to 40
  seconds per fight including screens; the playback speed of ticks must be tuned for it.
- Unique mini-boss loot is not defined yet (#68).

## Alternatives considered

- **Biome of about 5 or 12 minutes:** too short to grow the spell line, or too long to playtest the slice
  often.
- **Fixed sequence of fights or a branching map:** a fixed sequence is not replayable; a map costs much more
  work for the slice.
- **Health carried over (with or without healing on level-up):** more tension, but fights are no longer
  independent and the balance depends on the whole path.
- **One more slot per level, all cards in the line, or slots at fixed levels:** they tie the line size to
  levels instead of rewarding secret rooms.
- **Professor available from the start:** total freedom, but a hasty player can end the run at once.
- **Hard level cap or no limit on farming:** a cap feels like a wall; no limit lets the player outscale the
  professor.
- **Defeats outside the boss without consequence:** makes farming free of risk, against the strict roguelike
  rule.
