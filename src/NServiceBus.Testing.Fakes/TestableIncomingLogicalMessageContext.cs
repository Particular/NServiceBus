namespace NServiceBus.Testing
{
    using System.Collections.Generic;
    using Pipeline;
    using Unicast.Messages;

    /// <summary>
    /// A testable implementation of <see cref="IIncomingLogicalMessageContext" />.
    /// </summary>
    public partial class TestableIncomingLogicalMessageContext : TestableIncomingContext, IIncomingLogicalMessageContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="TestableIncomingLogicalMessageContext" />.
        /// </summary>
        public TestableIncomingLogicalMessageContext(IMessageCreator messageCreator = null) : base(messageCreator)
        {
        }

        /// <summary>
        /// Message being handled.
        /// </summary>
        public LogicalMessage Message { get; set; } = new LogicalMessage(new MessageMetadata(typeof(object)), new object());

        /// <summary>
        /// Headers for the incoming message.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Tells if the message has been handled.
        /// </summary>
        public bool MessageHandled { get; set; }

        /// <summary>
        /// Updates the message instance contained in <see cref="LogicalMessage" />.
        /// </summary>
        /// <param name="newInstance">The new instance.</param>
        public virtual void UpdateMessageInstance(object newInstance)
        {
            Message = new LogicalMessage(new MessageMetadata(newInstance.GetType()), newInstance);
        }
    }
}