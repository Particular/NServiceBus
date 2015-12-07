namespace NServiceBus.Pipeline.OutgoingPipeline
{
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Routing;

    /// <summary>
    /// Outgoing pipeline context.
    /// </summary>
    public interface OutgoingLogicalMessageContext : OutgoingContext
    {
        /// <summary>
        /// The outgoing message.
        /// </summary>
        OutgoingLogicalMessage Message { get; }

        /// <summary>
        /// The routing strategies for this message.
        /// </summary>
        IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; }

        /// <summary>
        /// Updates the message instance.
        /// </summary>
        void UpdateMessageInstance(object newInstance);
    }
}