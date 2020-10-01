namespace NServiceBus.Testing
{
    using System.Collections.Generic;
    using Pipeline;
    using Routing;

    /// <summary>
    /// A testable implementation of <see cref="IOutgoingLogicalMessageContext" />.
    /// </summary>
    public partial class TestableOutgoingLogicalMessageContext : TestableOutgoingContext, IOutgoingLogicalMessageContext
    {
        /// <summary>
        /// Updates the message instance.
        /// </summary>
        public virtual void UpdateMessage(object newInstance)
        {
            Message = new OutgoingLogicalMessage(newInstance.GetType(), newInstance);
        }

        /// <summary>
        /// The outgoing message.
        /// </summary>
        public OutgoingLogicalMessage Message { get; set; } = new OutgoingLogicalMessage(typeof(object), new object());

        /// <summary>
        /// The routing strategies for this message.
        /// </summary>
        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; set; } = new RoutingStrategy[0];
    }
}