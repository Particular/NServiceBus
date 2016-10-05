namespace NServiceBus
{
    using Routing;

    /// <summary>
    /// Defines the usage scope of a <see cref="DistributionStrategy"/>.
    /// </summary>
    public enum DistributionStrategyScope
    {

        /// <summary>
        /// All outgoing messages and commands, excluding events and subscription messages.
        /// </summary>
        Send,

        /// <summary>
        /// All published events.
        /// </summary>
        Publish
    }
}