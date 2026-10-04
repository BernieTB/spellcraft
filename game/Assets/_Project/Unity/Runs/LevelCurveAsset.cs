using System;
using System.Collections.Generic;
using Game.Core.Runs;
using Game.Unity.Content;
using UnityEngine;

namespace Game.Unity.Runs
{
    /// <summary>
    /// Authoring asset for the XP cost of each level-up (<c>docs/adr/0009-vertical-slice-run-pacing.md</c>):
    /// explicit costs, then a linear tail with no level cap. <see cref="ToDefinition"/> converts it to the Core
    /// <see cref="LevelCurve"/>, which describes the rules.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevelCurve", menuName = "Game/Level Curve")]
    public sealed class LevelCurveAsset : ScriptableObject, IPlaceholderContent
    {
        [SerializeField]
        [Tooltip("XP cost of each level-up from level 1, in order. Each cost greater than the one before.")]
        private List<int> _levelCosts = new List<int>();

        [SerializeField]
        [Min(1)]
        [Tooltip("Past the list, each level costs this much more than the previous one.")]
        private int _costIncreaseAfterList = 1;

        [SerializeField]
        [Tooltip("Test-only content. Tests and builds fail if an asset outside the placeholder and test folders references it.")]
        private bool _isPlaceholder;

        /// <inheritdoc />
        public bool IsPlaceholder => _isPlaceholder;

        /// <summary>Converts the asset to an immutable Core <see cref="LevelCurve"/>.</summary>
        /// <exception cref="InvalidOperationException">The asset data is invalid; the message names the asset.</exception>
        public LevelCurve ToDefinition()
        {
            try
            {
                return new LevelCurve(_levelCosts, _costIncreaseAfterList);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"Level curve asset '{name}' is invalid: {exception.Message}", exception);
            }
        }
    }
}
