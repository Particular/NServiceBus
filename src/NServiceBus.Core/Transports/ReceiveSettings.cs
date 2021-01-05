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
        public ReceiveSettings(string id, string receiveAddress, bool usePublishSubscribe, bool purgeOnStartup, string errorQueue)
        {
            Id = id;
            ReceiveAddress = receiveAddress;
            UsePublishSubscribe = usePublishSubscribe;
            PurgeOnStartup = purgeOnStartup;
            ErrorQueue = errorQueue;
        }

        /// <summary>
        /// A unique identifier for the related <see cref="IMessageReceiver"/>.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The queue address the <see cref="IMessageReceiver"/> should receive messages from.
        /// </summary>
        public string ReceiveAddress { get; set; }

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