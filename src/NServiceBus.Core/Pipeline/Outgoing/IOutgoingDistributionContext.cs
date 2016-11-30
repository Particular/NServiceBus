namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using Routing;

    /// <summary>
    /// Pipeline context for physical routing.
    /// </summary>
    public interface IOutgoingDistributionContext : IOutgoingContext
    {
        /// <summary>
        /// The outgoing message.
        /// </summary>
        OutgoingLogicalMessage Message { get; }

        /// <summary>
        /// The collection of logical endpoint names to which a message should be delivered.
        /// </summary>
        IReadOnlyCollection<UnicastRoute> Destinations { get; }

        /// <summary>
        /// The intent of the message.
        /// </summary>
        DistributionStrategyScope DistributionScope { get; }
    }
}