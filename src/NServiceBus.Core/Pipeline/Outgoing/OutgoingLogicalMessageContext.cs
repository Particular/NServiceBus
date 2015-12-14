namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Routing;

    /// <summary>
    /// Outgoing pipeline context.
    /// </summary>
    public class OutgoingLogicalMessageContext : OutgoingContext, IOutgoingLogicalMessageContext
    {
        /// <summary>
        /// Creates a new instance of an outgoing logical context.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="message">The outgoing logical message.</param>
        /// <param name="routingStrategies">The routing strategies.</param>
        /// <param name="parentContext">The parent context.</param>
        public OutgoingLogicalMessageContext(string messageId, Dictionary<string, string> headers, OutgoingLogicalMessage message, IReadOnlyCollection<RoutingStrategy> routingStrategies, IBehaviorContext parentContext)
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
        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; }

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