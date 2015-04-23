namespace NServiceBus.Transports
{
    /// <summary>
    /// Contains information necessary to set up a message pump for receiving messages.
    /// </summary>
    public class DequeueSettings
    {
        /// <summary>
        /// Creates an instance of <see cref="DequeueSettings"/>.
        /// </summary>
        /// <param name="queue">Queue name.</param>
        /// <param name="publicAddress"></param>
        /// <param name="purgeOnStartup"><code>true</code> to purge <paramref name="queue"/> at startup.</param>
        public DequeueSettings(string queue, string publicAddress, bool purgeOnStartup)
        {
            Guard.AgainstNullAndEmpty(queue, "queue");
            PurgeOnStartup = purgeOnStartup;
            QueueName = queue;
            PublicAddress = publicAddress;
        }

        /// <summary>
        /// The native queue to consume messages from
        /// </summary>
        public string QueueName{ get; private set; }

        /// <summary>
        /// Tells the dequeuer if the queue should be purged before starting to consume messages from it
        /// </summary>
        public bool PurgeOnStartup { get; private set; }

        /// <summary>
        /// An address other endpoints should use to sent messages to this endpoint.
        /// </summary>
        public string PublicAddress { get; private set; }
    }
}