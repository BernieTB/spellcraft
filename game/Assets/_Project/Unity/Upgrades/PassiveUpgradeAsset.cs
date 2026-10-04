using System;
using Game.Core.Effects;
using Game.Core.Upgrades;
using Game.Unity.Content;
using UnityEngine;

namespace Game.Unity.Upgrades
{
    /// <summary>
    /// Authoring asset for one passive upgrade (<c>docs/adr/0012-linked-choices-and-spell-line-editing.md</c>). Holds
    /// data only; <see cref="ToUpgrade"/> converts it to the Core <see cref="PassiveUpgrade"/>. Ids are placeholders
    /// until the owner writes content.
    /// </summary>
    /// <remarks>
    /// The enums are the Core ones, serialized as integers; their values are explicit and never renumbered.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewPassiveUpgrade", menuName = "Game/Passive Upgrade")]
    public sealed class PassiveUpgradeAsset : ScriptableObject, IPlaceholderContent
    {
        [SerializeField]
        [Tooltip("Stable identifier, unique among passive upgrades (placeholder ids such as test_upgrade_01 until content exists).")]
        private string _id;

        [SerializeField]
        [Tooltip("MaxHealth: +X max health. StartingShield: +X shield at the start of every fight. EffectAmount: +X to every effect of the effect kind below. NeighbourBonus: +X to every neighbour modifier of the hero's cards.")]
        private PassiveUpgradeKind _kind;

        [SerializeField]
        [Tooltip("Effect kind raised by an EffectAmount upgrade. Ignored by the other kinds.")]
        private BonusKind _effectKind;

        [SerializeField]
        [Min(0)]
        [Tooltip("The X of the upgrade. Taking the same upgrade again adds it again.")]
        private int _amount;

        [SerializeField]
        [Tooltip("Test-only content. Tests and builds fail if an asset outside the placeholder and test folders references it.")]
        private bool _isPlaceholder;

        /// <inheritdoc />
        public bool IsPlaceholder => _isPlaceholder;

        /// <summary>
        /// Converts this asset to an immutable Core passive upgrade.
        /// </summary>
        /// <exception cref="InvalidOperationException">The asset data is invalid; the message names the asset.</exception>
        public PassiveUpgrade ToUpgrade()
        {
            try
            {
                return _kind == PassiveUpgradeKind.EffectAmount
                    ? new PassiveUpgrade(_id, _kind, _effectKind, _amount)
                    : new PassiveUpgrade(_id, _kind, _amount);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"Passive upgrade asset '{name}' is invalid: {exception.Message}", exception);
            }
        }
    }
}
