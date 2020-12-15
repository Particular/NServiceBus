using System.Collections.Generic;
using NServiceBus.Unicast.Messages;

namespace NServiceBus.Transport
{
    /// <summary>
    /// 
    /// </summary>
    public class ReceiveSettings
    {
        /// <summary>
        /// </summary>
        public ReceiveSettings(string id, string receiveAddress, bool usePublishSubscribe, bool purgeOnStartup, string errorQueue, TransportTransactionMode requiredTransactionMode, IReadOnlyCollection<MessageMetadata> events)
        {
            Id = id;
            ReceiveAddress = receiveAddress;
            UsePublishSubscribe = usePublishSubscribe;
            PurgeOnStartup = purgeOnStartup;
            ErrorQueue = errorQueue;
            RequiredTransactionMode = requiredTransactionMode;
            Events = events;
        }

        /// <summary>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ReceiveAddress { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool UsePublishSubscribe { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        /// <summary>
        /// The native queue where to send corrupted messages to.
        /// </summary>
        public string ErrorQueue { get; }

        /// <summary>
        /// The transaction mode required for receive operations.
        /// </summary>
        public TransportTransactionMode RequiredTransactionMode { get; }

        /// <summary>
        /// A list of events that this endpoint is handling.
        /// </summary>
        public IReadOnlyCollection<MessageMetadata> Events { get; }
    }
}