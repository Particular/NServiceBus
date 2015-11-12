namespace NServiceBus.Pipeline.OutgoingPipeline
{
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Routing;

    /// <summary>
    /// Outgoing pipeline context.
    /// </summary>
    public class OutgoingLogicalMessageContext : OutgoingContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="OutgoingLogicalMessageContext" />.
        /// </summary>
        /// <param name="message">The outgoing message.</param>
        /// <param name="routingStrategies">The address labels.</param>
        /// <param name="parentContext">The parent context.</param>
        public OutgoingLogicalMessageContext(OutgoingLogicalMessage message, IReadOnlyCollection<RoutingStrategy> routingStrategies, BehaviorContext parentContext)
            : base(parentContext)
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