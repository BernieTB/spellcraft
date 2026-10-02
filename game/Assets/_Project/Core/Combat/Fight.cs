using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Cards;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;

namespace Game.Core.Combat
{
    /// <summary>
    /// A deterministic fight between one hero and one or more enemies, each casting its own spell line in a loop.
    /// Create it with the participants, a maximum number of ticks and a seeded random source, then call
    /// <see cref="Run"/> once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rules (<c>docs/adr/0002-first-pass-combat-rules.md</c>, including its provisional details):
    /// </para>
    /// <list type="bullet">
    /// <item>The fight advances in discrete ticks, numbered from 1. Nothing reads wall-clock time.</item>
    /// <item>Every living combatant casts the cards of its spell line in order, looping from the last card back to
    /// the first, starting at position 0. A card resolves on the tick its cast time has elapsed (a card with cast
    /// time <c>c</c> started on tick <c>s</c> resolves on tick <c>s + c - 1</c>); the next card starts on the
    /// following tick.</item>
    /// <item>On one tick, combatants are processed in a fixed order: the hero first, then enemies in the order
    /// given. A combatant killed earlier in the tick does not resolve its cast.</item>
    /// <item>A dead combatant stops casting. The fight ends as soon as the hero is dead (enemies win) or every
    /// enemy is dead (hero wins), even in the middle of a tick.</item>
    /// <item>If neither happens within the maximum number of ticks, the fight ends with no winner (timeout).</item>
    /// </list>
    /// <para>
    /// Targeting (provisional, not specified by the owner yet): the hero aims at the first living enemy in order;
    /// every enemy aims at the hero. Effects that act on the caster (heal, shield) ignore the target.
    /// </para>
    /// <para>
    /// Spell lines are copied when the fight is created, so editing a line afterwards does not change the fight.
    /// Combatants are not copied: the fight changes their health and shield.
    /// </para>
    /// <para>
    /// Every resolution produces a <see cref="CastRecord"/> in <see cref="FightResult.Casts"/>; that is the
    /// intended hook for the combat event log. Neighbour modifiers are not applied yet.
    /// </para>
    /// </remarks>
    public sealed class Fight
    {
        /// <summary>Fight index of the hero. Enemies follow, from 1 to N, in the order given.</summary>
        public const int HeroIndex = 0;

        private readonly Combatant[] _combatants;
        private readonly SpellLine<CardDefinition>[] _spellLines;
        private readonly int[] _positions;
        private readonly int[] _elapsedTicks;
        private readonly int _maxTicks;
        private bool _hasRun;

        /// <param name="hero">The player's side.</param>
        /// <param name="enemies">The enemies, at least one, in resolution order.</param>
        /// <param name="maxTicks">
        /// Guard against endless fights: the fight stops with no winner after this many ticks. From data or the
        /// caller, at least 1.
        /// </param>
        /// <param name="random">
        /// Seeded random source for the fight. No first-pass rule is random, so it is not used yet; it is injected
        /// now so random rules can be added without changing callers.
        /// </param>
        /// <exception cref="ArgumentNullException">An argument or an enemy is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enemies"/> is empty, a spell line is empty, a combatant is already dead, or the same
        /// combatant appears twice.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxTicks"/> is less than 1.</exception>
        public Fight(FightParticipant hero, IReadOnlyList<FightParticipant> enemies, int maxTicks, IRandom random)
        {
            if (hero == null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            if (enemies.Count == 0)
            {
                throw new ArgumentException("A fight needs at least one enemy.", nameof(enemies));
            }

            if (maxTicks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTicks), maxTicks, "Max ticks must be at least 1.");
            }

            Random = random ?? throw new ArgumentNullException(nameof(random));
            _maxTicks = maxTicks;

            var participants = new List<FightParticipant>(enemies.Count + 1) { hero };
            for (var i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null)
                {
                    throw new ArgumentNullException(nameof(enemies), $"Enemy {i} is null.");
                }

                participants.Add(enemies[i]);
            }

            var count = participants.Count;
            _combatants = new Combatant[count];
            _spellLines = new SpellLine<CardDefinition>[count];
            _positions = new int[count];
            _elapsedTicks = new int[count];

            for (var i = 0; i < count; i++)
            {
                var participant = participants[i];
                var paramName = i == HeroIndex ? nameof(hero) : nameof(enemies);

                if (participant.SpellLine.IsEmpty)
                {
                    throw new ArgumentException($"Combatant {i} has an empty spell line.", paramName);
                }

                if (participant.Combatant.IsDead)
                {
                    throw new ArgumentException($"Combatant {i} is already dead.", paramName);
                }

                if (Array.IndexOf(_combatants, participant.Combatant, 0, i) >= 0)
                {
                    throw new ArgumentException($"Combatant {i} appears more than once in the fight.", paramName);
                }

                _combatants[i] = participant.Combatant;
                _spellLines[i] = Copy(participant.SpellLine);
            }
        }

        /// <summary>The seeded random source of the fight. Unused by the first-pass rules.</summary>
        public IRandom Random { get; }

        /// <summary>
        /// Runs the fight to its end: a side wins or the maximum number of ticks is reached.
        /// </summary>
        /// <returns>The winner, the number of ticks and every resolved cast.</returns>
        /// <exception cref="InvalidOperationException">The fight has already been run.</exception>
        public FightResult Run()
        {
            if (_hasRun)
            {
                throw new InvalidOperationException("A fight can only be run once.");
            }

            _hasRun = true;
            var casts = new List<CastRecord>();
            var winner = FightWinner.None;
            var tick = 0;

            while (winner == FightWinner.None && tick < _maxTicks)
            {
                tick++;
                for (var i = 0; i < _combatants.Length; i++)
                {
                    if (_combatants[i].IsDead)
                    {
                        continue;
                    }

                    _elapsedTicks[i]++;
                    var position = _positions[i];
                    var card = _spellLines[i][position];
                    if (_elapsedTicks[i] < card.CastTime)
                    {
                        continue;
                    }

                    var targetIndex = SelectTarget(i);
                    var outcomes = card.Resolve(new EffectContext(_combatants[i], _combatants[targetIndex]));
                    casts.Add(new CastRecord(tick, i, position, card, targetIndex, outcomes));

                    _positions[i] = _spellLines[i].PositionAfter(position);
                    _elapsedTicks[i] = 0;

                    winner = CurrentWinner();
                    if (winner != FightWinner.None)
                    {
                        break;
                    }
                }
            }

            return new FightResult(winner, tick, new ReadOnlyCollection<CastRecord>(casts));
        }

        // Provisional targeting rule: the hero aims at the first living enemy, enemies aim at the hero.
        // Only called while the fight is not over, so a living enemy always exists.
        private int SelectTarget(int casterIndex)
        {
            if (casterIndex != HeroIndex)
            {
                return HeroIndex;
            }

            for (var i = HeroIndex + 1; i < _combatants.Length; i++)
            {
                if (!_combatants[i].IsDead)
                {
                    return i;
                }
            }

            throw new InvalidOperationException("No living enemy to target.");
        }

        private FightWinner CurrentWinner()
        {
            if (_combatants[HeroIndex].IsDead)
            {
                return FightWinner.Enemies;
            }

            for (var i = HeroIndex + 1; i < _combatants.Length; i++)
            {
                if (!_combatants[i].IsDead)
                {
                    return FightWinner.None;
                }
            }

            return FightWinner.Hero;
        }

        private static SpellLine<CardDefinition> Copy(SpellLine<CardDefinition> source)
        {
            var copy = new SpellLine<CardDefinition>(source.Capacity);
            foreach (var card in source.Cards)
            {
                copy.Add(card);
            }

            return copy;
        }
    }
}
