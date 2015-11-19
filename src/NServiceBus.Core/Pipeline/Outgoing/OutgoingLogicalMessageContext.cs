namespace NServiceBus.Pipeline.OutgoingPipeline
{
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Routing;

    /// <summary>
    /// Outgoing pipeline context.
    /// </summary>
    public interface OutgoingLogicalMessageContext : IOutgoingContext
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

    /// <summary>
    /// Outgoing pipeline context.
    /// </summary>
    public class OutgoingLogicalMessageContextImpl : OutgoingContext, OutgoingLogicalMessageContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="OutgoingLogicalMessageContextImpl" />.
        /// </summary>
        /// <param name="messageId">The ID of the outgoing message.</param>
        /// <param name="headers">The headers of the outgoing message.</param>
        /// <param name="message">The outgoing message.</param>
        /// <param name="routingStrategies">The address labels.</param>
        /// <param name="parentContext">The parent context.</param>
        public OutgoingLogicalMessageContextImpl(string messageId, Dictionary<string, string> headers, OutgoingLogicalMessage message,  IReadOnlyCollection<RoutingStrategy> routingStrategies, BehaviorContext parentContext)
            : base(messageId, headers, parentContext)
        {
            Message = message;
            RoutingStrategies = routingStrategies;
            Set(message);
        }

        /// <summary>
        /// The outgoing message.
        /// </summary>
        public OutgoingLogicalMessage Message { get; private set; }

        /// <summary>
        /// The routing strategies for this message.
        /// </summary>
        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; private set; }

        /// <summary>
        /// Updates the message instance.
        /// </summary>
        public void UpdateMessageInstance(object newInstance)
        {
            Guard.AgainstNull(nameof(newInstance), newInstance);

            Message = new OutgoingLogicalMessage(newInstance);
        }
    }
}