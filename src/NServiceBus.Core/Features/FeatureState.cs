namespace NServiceBus.Features
{
    /// <summary>
    /// Defines state of a feature.
    /// </summary>
    public enum FeatureState
    {
        /// <summary>
        /// Not selected for activation.
        /// </summary>
        Disabled,

        /// <summary>
        /// Selected for activation.
        /// </summary>
        Enabled,

        /// <summary>
        /// Activated.
        /// </summary>
        Active,

        /// <summary>
        /// Activation not possible.
        /// </summary>
        Deactivated
    }
}