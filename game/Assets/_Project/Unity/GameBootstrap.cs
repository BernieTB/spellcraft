using Game.Core;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>
    /// Entry point bridging Unity and the pure Core module.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log($"[Spellcraft] {CoreInfo.ModuleName} loaded.");
        }
    }
}
