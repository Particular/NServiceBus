namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using Routing;

    /// <summary>
    /// Outgoing pipeline context.
    /// </summary>
    public interface IOutgoingLogicalMessageContext : IOutgoingContext
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
        void UpdateMessage(object newInstance);
    }
}