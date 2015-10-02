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
        /// The address labels for this message.
        /// </summary>
        public IReadOnlyCollection<AddressLabel> AddressLabels { get; }

        /// <summary>
        /// Creates a new instance of <see cref="OutgoingLogicalMessageContext"/>.
        /// </summary>
        /// <param name="message">The outgoing message.</param>
        /// <param name="addressLabels">The address labels.</param>
        /// <param name="parentContext">The parent context.</param>
        public OutgoingLogicalMessageContext(OutgoingLogicalMessage message, IReadOnlyCollection<AddressLabel> addressLabels, ContextBag parentContext)
            : base(parentContext)
        {
            Message = message;
            AddressLabels = addressLabels;
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