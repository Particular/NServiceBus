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
        /// <param name="purgeOnStartup"><code>true</code> to purge <paramref name="queue"/> at startup.</param>
        public DequeueSettings(string queue, bool purgeOnStartup)
        {
            Guard.AgainstNullAndEmpty(queue, "queue");
            PurgeOnStartup = purgeOnStartup;
            QueueName = queue;
        }

        /// <summary>
        /// The native queue to consume messages from
        /// </summary>
        public string QueueName{ get; private set; }

        /// <summary>
        /// Tells the dequeuer if the queue should be purged before starting to consume messages from it
        /// </summary>
        public bool PurgeOnStartup { get; private set; }
    }
}