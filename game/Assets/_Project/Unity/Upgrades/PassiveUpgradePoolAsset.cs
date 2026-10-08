using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Upgrades;
using UnityEngine;

namespace Game.Unity.Upgrades
{
    /// <summary>
    /// Authoring asset for the pool of passive upgrades that level-up offers draw from
    /// (<c>docs/adr/0012-linked-choices-and-spell-line-editing.md</c>). The caller of <c>Run.GetLevelUpOffer</c>
    /// passes <see cref="ToUpgrades"/> as the passive pool. Holds data only.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPassiveUpgradePool", menuName = "Game/Passive Upgrade Pool")]
    public sealed class PassiveUpgradePoolAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Stable identifier, unique among passive upgrade pools.")]
        private string _id;

        [SerializeField]
        [Tooltip("The passive upgrades that level-up offers can draw from. Not empty.")]
        private List<PassiveUpgradeAsset> _upgrades = new List<PassiveUpgradeAsset>();

        /// <summary>Stable identifier of the pool.</summary>
        public string Id => _id;

        /// <summary>The upgrade assets of the pool, in order.</summary>
        public IReadOnlyList<PassiveUpgradeAsset> Upgrades => _upgrades;

        /// <summary>
        /// Converts the pool to Core passive upgrades, in order.
        /// </summary>
        /// <exception cref="InvalidOperationException">The pool is empty, holds an empty slot, or an asset is invalid.</exception>
        public IReadOnlyList<PassiveUpgrade> ToUpgrades()
        {
            if (_upgrades == null || _upgrades.Count == 0)
            {
                throw new InvalidOperationException($"Passive upgrade pool asset '{name}' is empty.");
            }

            if (_upgrades.Any(upgrade => upgrade == null))
            {
                throw new InvalidOperationException($"Passive upgrade pool asset '{name}' has an empty slot.");
            }

            return _upgrades.Select(upgrade => upgrade.ToUpgrade()).ToList();
        }
    }
}
