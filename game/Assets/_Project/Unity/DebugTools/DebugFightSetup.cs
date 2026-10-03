using System;
using System.Collections.Generic;
using Game.Core.Combat;
using Game.Core.Combat.Log;
using Game.Core.Randomness;
using Game.Unity.Cards;
using Game.Unity.Combat;
using UnityEngine;

namespace Game.Unity.DebugTools
{
    /// <summary>
    /// Data for the debug fight viewer: the hero, the enemies, the seed, the tick limit and the playback rate.
    /// Every number of the debug fight lives in this asset. <see cref="Simulate"/> maps it to Core and records the
    /// fight's <see cref="CombatLog"/>; the rules are entirely in Core.
    /// </summary>
    /// <remarks>
    /// Debug only: the asset may reference placeholder cards, so it must stay in
    /// <c>Assets/_Project/Unity/DebugTools/</c> and out of player builds (enforced by the placeholder build check).
    /// </remarks>
    [CreateAssetMenu(fileName = "DebugFightSetup", menuName = "Game/Debug/Fight Setup")]
    public sealed class DebugFightSetup : ScriptableObject
    {
        [SerializeField]
        [Tooltip("The player's side.")]
        private CombatantSetup _hero = new CombatantSetup();

        [SerializeField]
        [Tooltip("The enemies, in resolution order. At least one.")]
        private List<CombatantSetup> _enemies = new List<CombatantSetup>();

        [SerializeField]
        [Tooltip("Seed of the fight's random source.")]
        private long _seed;

        [SerializeField]
        [Min(1)]
        [Tooltip("The fight stops with no winner after this many ticks.")]
        private int _maxTicks = 1;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Playback rate at speed x1, in ticks per second of real time.")]
        private float _ticksPerSecond = 1f;

        /// <summary>Playback rate at speed x1, in ticks per second of real time.</summary>
        public float TicksPerSecond => _ticksPerSecond;

        /// <summary>Seed of the debug fight. The headless simulation runner starts its seed range here by default.</summary>
        public long Seed => _seed;

        /// <summary>
        /// Builds the fight described by this asset, runs it in Core and returns its log.
        /// </summary>
        /// <exception cref="InvalidOperationException">The asset data is invalid; the message names the asset.</exception>
        public CombatLog Simulate()
        {
            return Guarded(() =>
            {
                var (hero, enemies) = CreateParticipants();
                return CombatLogRecorder.Record(hero, enemies, _maxTicks, CreateRandom(_seed));
            });
        }

        /// <summary>
        /// Builds a fresh, not yet run, fight from this asset with <paramref name="seed"/> instead of the asset's own
        /// seed. Used by the headless simulation runner to play the same setup over a range of seeds.
        /// </summary>
        /// <exception cref="InvalidOperationException">The asset data is invalid; the message names the asset.</exception>
        public Fight CreateFight(long seed)
        {
            return Guarded(() =>
            {
                var (hero, enemies) = CreateParticipants();
                return new Fight(hero, enemies, _maxTicks, CreateRandom(seed));
            });
        }

        // The seed is a long in the inspector; the random source takes its bits as an unsigned value.
        private static IRandom CreateRandom(long seed) => new Pcg32Random(unchecked((ulong)seed));

        private (FightParticipant Hero, List<FightParticipant> Enemies) CreateParticipants()
        {
            var hero = _hero.ToParticipant("hero");
            var enemies = new List<FightParticipant>(_enemies.Count);
            for (var i = 0; i < _enemies.Count; i++)
            {
                enemies.Add(_enemies[i].ToParticipant($"enemy {i + 1}"));
            }

            return (hero, enemies);
        }

        private T Guarded<T>(Func<T> build)
        {
            try
            {
                return build();
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                throw new InvalidOperationException($"Debug fight setup '{name}' is invalid: {exception.Message}", exception);
            }
        }

        /// <summary>Stats and spell line of one combatant of the debug fight.</summary>
        [Serializable]
        public sealed class CombatantSetup
        {
            [SerializeField]
            [Min(1)]
            private int _maxHealth = 1;

            [SerializeField]
            [Min(0)]
            private int _startingShield;

            [SerializeField]
            [Tooltip("Cards of the spell line, in order. At least one.")]
            private List<CardAsset> _spellLine = new List<CardAsset>();

            internal FightParticipant ToParticipant(string label)
            {
                return FightParticipantBuilder.Create(_maxHealth, _startingShield, _spellLine, label);
            }
        }
    }
}
