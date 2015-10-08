namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using Extensibility;
    using NServiceBus.Routing;
    using OutgoingPipeline;

    /// <summary>
    /// Outgoing pipeline context.
    /// </summary>
    public class OutgoingLogicalMessageContext : BehaviorContext
    {
        /// <summary>
        /// The outgoing message.
        /// </summary>
        public OutgoingLogicalMessage Message { get; private set; }

        /// <summary>
        /// The routing strategies for this message.
        /// </summary>
        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="OutgoingLogicalMessageContext"/>.
        /// </summary>
        /// <param name="message">The outgoing message.</param>
        /// <param name="routingStrategies">The address labels.</param>
        /// <param name="parentContext">The parent context.</param>
        public OutgoingLogicalMessageContext(OutgoingLogicalMessage message, IReadOnlyCollection<RoutingStrategy> routingStrategies, ContextBag parentContext)
            : base(parentContext)
        {
            Message = message;
            RoutingStrategies = routingStrategies;
            Set(message);
        }

        /// <summary>
        /// Updates the message instance.
        /// </summary>
        public void UpdateMessageInstance(object newInstance)
        {
            Guard.AgainstNull("newInstance", newInstance);

            Message = new OutgoingLogicalMessage(newInstance);
        }
    }
}