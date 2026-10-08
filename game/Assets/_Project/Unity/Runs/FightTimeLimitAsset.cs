using Game.Unity.Content;
using UnityEngine;

namespace Game.Unity.Runs
{
    /// <summary>
    /// The global fight time limit (ADR 0011): one value in data, far above the length of a normal fight, read by the
    /// run (<c>RunRules.FightTimeLimit</c>). A fight that reaches it counts as a defeat of the hero.
    /// </summary>
    [CreateAssetMenu(fileName = "FightTimeLimit", menuName = "Game/Fight Time Limit")]
    public sealed class FightTimeLimitAsset : ScriptableObject, IPlaceholderContent
    {
        [SerializeField]
        [Min(1)]
        [Tooltip("Maximum length of a fight in ticks. A safety net, never reached in normal play.")]
        private int _maxTicks = 1000;

        [SerializeField]
        [Tooltip("Test-only content. Tests and builds fail if an asset outside the placeholder and test folders references it.")]
        private bool _isPlaceholder;

        /// <summary>Maximum length of a fight in ticks.</summary>
        public int MaxTicks => _maxTicks;

        public bool IsPlaceholder => _isPlaceholder;
    }
}
