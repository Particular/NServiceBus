namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using Persistence;
    using Unicast.Messages;

    /// <summary>
    /// A context for creating setting up custom unit of work for handlers.
    /// </summary>
    public interface IUnitOfWorkContext : IIncomingContext
    {
        /// <summary>
        /// Gets the synchronized storage session for processing the current message. NServiceBus makes sure the changes made
        /// via this session will be persisted before the message receive is acknowledged.
        /// </summary>
        SynchronizedStorageSession SynchronizedStorageSession { get; }

        /// <summary>
        /// The message instance being handled.
        /// </summary>
        object MessageBeingHandled { get; }

        /// <summary>
        /// Message headers.
        /// </summary>
        Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Metadata for the incoming message.
        /// </summary>
        MessageMetadata MessageMetadata { get; }

        /// <summary>
        /// Tells if the message has been handled.
        /// </summary>
        bool MessageHandled { get; set; }
    }
}