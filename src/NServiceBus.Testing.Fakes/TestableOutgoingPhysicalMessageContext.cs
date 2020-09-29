namespace NServiceBus.Testing
{
    using System.Collections.Generic;
    using Pipeline;
    using Routing;

    /// <summary>
    /// A testable implementation of <see cref="IOutgoingPhysicalMessageContext" />.
    /// </summary>
    public partial class TestableOutgoingPhysicalMessageContext : TestableOutgoingContext, IOutgoingPhysicalMessageContext
    {
        /// <summary>
        /// Updates the message with the given body.
        /// </summary>
        public virtual void UpdateMessage(byte[] body)
        {
            Body = body;
        }

        /// <summary>
        /// The serialized body of the outgoing message.
        /// </summary>
        /// <summary>
        /// A <see cref="T:System.Byte" /> array containing the serialized contents of the outgoing message.
        /// </summary>
        public byte[] Body { get; set; } = new byte[0];

        /// <summary>
        /// The routing strategies for this message.
        /// </summary>
        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; set; } = new RoutingStrategy[0];
    }
}