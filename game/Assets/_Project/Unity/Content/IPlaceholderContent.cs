namespace Game.Unity.Content
{
    /// <summary>
    /// Content asset that can be flagged as test-only placeholder. The editor placeholder guard (a build check and an
    /// EditMode test) keeps flagged assets, and every asset that depends on one, out of shipped content.
    /// </summary>
    public interface IPlaceholderContent
    {
        /// <summary>True for test-only placeholder content, which must never ship. Not used by the simulation.</summary>
        bool IsPlaceholder { get; }
    }
}
