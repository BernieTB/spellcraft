using System;
using System.Collections.Generic;
using Game.Core.Runs;
using Game.Core.Upgrades;
using Game.Unity.Classes;
using Game.Unity.Runs;
using Game.Unity.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.Flow
{
    /// <summary>
    /// What the playable game is made of (#88): the class and the biome of a run, its rules, the passive upgrade pool
    /// and the layout of every screen. The bootstrap scene references this one asset, so a player build contains
    /// exactly what it reaches (no <c>Resources</c> folder, no path in code). The assets it points to must not be
    /// placeholder content: the placeholder guard fails a build otherwise.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Config")]
    public sealed class GameConfigAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("The class of every run (one class in the Vertical slice).")]
        private ClassAsset _heroClass;

        [SerializeField]
        [Tooltip("The biome of every run (one biome in the Vertical slice).")]
        private BiomeAsset _biome;

        [SerializeField]
        [Tooltip("The global fight time limit.")]
        private FightTimeLimitAsset _fightTimeLimit;

        [SerializeField]
        [Tooltip("The experience curve.")]
        private LevelCurveAsset _levelCurve;

        [SerializeField]
        [Tooltip("The passive upgrades the level-up offers draw from.")]
        private PassiveUpgradePoolAsset _passivePool;

        [SerializeField]
        private VisualTreeAsset _titleScreen;

        [SerializeField]
        private VisualTreeAsset _runScreen;

        [SerializeField]
        private VisualTreeAsset _levelUpScreen;

        [SerializeField]
        private VisualTreeAsset _preparationScreen;

        [SerializeField]
        private VisualTreeAsset _recapScreen;

        [SerializeField]
        private VisualTreeAsset _endScreen;

        public VisualTreeAsset TitleScreen => _titleScreen;

        public VisualTreeAsset RunScreen => _runScreen;

        public VisualTreeAsset LevelUpScreen => _levelUpScreen;

        public VisualTreeAsset PreparationScreen => _preparationScreen;

        public VisualTreeAsset RecapScreen => _recapScreen;

        public VisualTreeAsset EndScreen => _endScreen;

        /// <summary>Creates the run of the game for <paramref name="seed"/>: the class, the biome and the rules of this config.</summary>
        /// <exception cref="InvalidOperationException">A reference is missing or an asset is invalid.</exception>
        public Run CreateRun(ulong seed)
        {
            Require(_heroClass, nameof(_heroClass));
            Require(_biome, nameof(_biome));
            Require(_fightTimeLimit, nameof(_fightTimeLimit));
            Require(_levelCurve, nameof(_levelCurve));
            var rules = new RunRules(_fightTimeLimit.MaxTicks, _levelCurve.ToDefinition());
            return new Run(_heroClass.ToDefinition(), _biome.ToDefinition(), rules, seed);
        }

        /// <summary>The passive upgrades the level-up offers draw from.</summary>
        /// <exception cref="InvalidOperationException">The pool is missing.</exception>
        public IReadOnlyList<PassiveUpgrade> ToPassivePool()
        {
            Require(_passivePool, nameof(_passivePool));
            return _passivePool.ToUpgrades();
        }

        /// <summary>Checks that every reference is set; the message names the first one missing.</summary>
        /// <exception cref="InvalidOperationException">A reference is missing.</exception>
        public void Validate()
        {
            Require(_heroClass, nameof(_heroClass));
            Require(_biome, nameof(_biome));
            Require(_fightTimeLimit, nameof(_fightTimeLimit));
            Require(_levelCurve, nameof(_levelCurve));
            Require(_passivePool, nameof(_passivePool));
            Require(_titleScreen, nameof(_titleScreen));
            Require(_runScreen, nameof(_runScreen));
            Require(_levelUpScreen, nameof(_levelUpScreen));
            Require(_preparationScreen, nameof(_preparationScreen));
            Require(_recapScreen, nameof(_recapScreen));
            Require(_endScreen, nameof(_endScreen));
        }

        private void Require(UnityEngine.Object reference, string field)
        {
            if (reference == null)
            {
                throw new InvalidOperationException($"Game config '{name}' has no '{field}': rebuild it with Tools > Game > Rebuild Bootstrap Scene.");
            }
        }
    }
}
