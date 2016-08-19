namespace NServiceBus
{
    using Routing;

    /// <summary>
    /// Defines the scope of a <see cref="DistributionStrategy"/>.
    /// </summary>
    public enum DistributionStrategyScope
    {

        /// <summary>
        /// All outgoing messages and commands excluding events and subscription messages.
        /// </summary>
        Sends,

        /// <summary>
        /// All published messages.
        /// </summary>
        Publishes
    }
}