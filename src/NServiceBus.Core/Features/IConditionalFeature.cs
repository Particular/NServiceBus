namespace NServiceBus.Features
{
    /// <summary>
    /// Used be features that needs to check for certain conditions to be true for it to run
    /// </summary>
    public interface IConditionalFeature:IFeature
    {
        /// <summary>
        /// Returns true if the feature should be enable. This method wont be called if the feature is explicitly disabled
        /// </summary>
        /// <returns></returns>
        bool ShouldBeEnabled();
    }
}