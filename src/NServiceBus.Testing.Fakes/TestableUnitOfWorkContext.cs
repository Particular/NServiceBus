namespace NServiceBus.Testing
{
    using System.Collections.Generic;
    using Persistence;
    using Pipeline;
    using Unicast.Messages;

    /// <summary>
    /// A testable implementation of <see cref="IUnitOfWorkContext"/>
    /// </summary>
    public class TestableUnitOfWorkContext : TestableIncomingContext, IUnitOfWorkContext
    {
        /// <summary>
        /// Gets the synchronized storage session for processing the current message. NServiceBus makes sure the changes made
        /// via this session will be persisted before the message receive is acknowledged.
        /// </summary>
        public SynchronizedStorageSession SynchronizedStorageSession { get; set; }

        /// <summary>
        /// The message instance being handled.
        /// </summary>
        public object MessageBeingHandled { get; set; } = new object();

        /// <summary>
        /// Message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Metadata for the incoming message.
        /// </summary>
        public MessageMetadata MessageMetadata { get; set; } = new MessageMetadata(typeof(object));

        /// <summary>
        /// Tells if the message has been handled.
        /// </summary>
        public bool MessageHandled { get; set; }
    }
}