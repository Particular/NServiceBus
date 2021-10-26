namespace NServiceBus.Transport
{
    /// <summary>
    /// Settings that belong to a specific <see cref="IMessageReceiver"/>.
    /// </summary>
    public class ReceiveSettings
    {
        /// <summary>
        /// Creates a new instance of <see cref="ReceiveSettings"/>.
        /// </summary>
        public ReceiveSettings(string id, QueueAddress receiveName, bool usePublishSubscribe, bool purgeOnStartup, string errorQueue)
        {
            Id = id;
            UsePublishSubscribe = usePublishSubscribe;
            PurgeOnStartup = purgeOnStartup;
            ErrorQueue = errorQueue;
            ReceiverName = receiveName;
        }

        /// <summary>
        /// A unique identifier for the related <see cref="IMessageReceiver"/>.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The logical name of the receiver. The name will be translated by <see cref="TransportInfrastructure.ToTransportAddress"/>.
        /// </summary>
        public QueueAddress ReceiverName { get; set; }

        /// <summary>
        /// A flag indicating whether events will be subscribed to this receiver's queue.
        /// </summary>
        public bool UsePublishSubscribe { get; set; }

        /// <summary>
        /// A flag indicating whether the queue should be purged before the receiver starts.
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        /// <summary>
        /// The native queue where to send corrupted messages to.
        /// </summary>
        public string ErrorQueue { get; }
    }
}