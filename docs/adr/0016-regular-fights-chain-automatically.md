# 0016. Regular fights chain automatically

- **Status:** Accepted
- **Date:** 2026-10-09

## Context

[ADR 0009](0009-vertical-slice-run-pacing.md) said that, between fights, the player picks the next step (a
regular fight, an unlocked secret room or the professor) and starts every fight with a click. The owner played the
Vertical slice build (feedback on #124, 2026-10-09) and wants the opposite for regular fights: they are the
"idle" part of the game, an auto-battler where fights follow one another by themselves and bonuses come as the
character levels up. Only the choices that matter (level-up, secret rooms, the professor) should ask for the
player.

## Decision

This ADR amends ADR 0009: **the player no longer chooses or starts each regular fight.** Everything else in ADR 0009
stays (defeat ends the run, health reset, the minimum number of regular fights before the professor, farming).

**Regular fights chain.** After a won regular fight, a short pause follows, then the next regular fight starts by
itself, with no click. The pause is a presentation setting (`RunScreenSettings.NextFightDelaySeconds`, about 1.5
seconds at speed x1; it shortens with the playback speed and stops while paused), not a balance number. The
encounter is still drawn from the seed when the fight starts.

**A level-up stops the loop.** When a fight leaves a level-up waiting, the loop stops and the level-up choice
screen opens (the choice stays mandatory, ADR 0012). Once it is taken, the loop goes on with the next regular fight.

**Secret rooms and the professor are entered on demand.** Their buttons are always visible on the run screen when the
step exists (the professor shows why it is locked until the minimum number of regular fights is won). Pressing one
at any time **requests** the step: it is entered **after the current fight**, through the preparation screen as
before (ADR 0014). Pressing the same button again withdraws the request. While nothing is requested, the loop
simply goes on. When a level-up and a request are both waiting, the level-up comes first.

**Recaps.** A won regular fight shows no recap: the run screen keeps going (the status line shows the result and
the countdown). The recap is shown after every mini-boss and professor fight and after any defeat, as ADR 0014
already says. After a recap the loop goes on.

**Unchanged.** A defeat ends the run. The player can edit the line live during regular fights (ADR 0012 and 0015).
The speed control and the pause stay; the pause also stops the loop countdown. Mini-boss and professor fights
still go through the preparation screen and have a fixed line.

## Consequences

### Positive

- The regular part plays itself, as the "idle" pillar of the game asks; the player attention goes to the line,
  the level-ups and the decision of when to attempt a mini-boss or the professor.
- The loop is plain C# in the run screen controller and the game flow, so it is tested without a scene.

### Negative

- A player can no longer pace the run by delaying a regular fight: only Pause stops the loop, and a level-up
  choice is the only forced stop. Farming becomes the default and not a choice: the player must press the professor
  button to end the biome.
- The total time of a biome depends on the pause and the playback speed; the target of 30 to 40 seconds per
  fight including screens in ADR 0009 must be tuned again (the pause adds to it).
- The "Leave fight" button now only abandons the current fight; for a regular fight another one follows.

## Alternatives considered

- **Keep the step choice and add an "auto" toggle:** more options on screen for a behaviour the owner wants as the
  default.
- **Recap after every fight, with a click:** it breaks the idle flow; the recap is worth its click for boss fights and
  defeats (ADR 0014).
- **Enter a room or the professor at once when pressed:** it would cancel the fight in progress, which does not
  count and loses its XP; "after the current fight" is safer.
