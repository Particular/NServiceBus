namespace NServiceBus.Transport
{
    /// <summary>
    /// Contains information necessary to set up a message pump for receiving messages.
    /// </summary>
    public class PushSettings
    {
        /// <summary>
        /// Creates an instance of <see cref="PushSettings" />.
        /// </summary>
        /// <param name="inputQueue">Input queue name.</param>
        /// <param name="purgeOnStartup"><code>true</code> to purge <paramref name="inputQueue" /> at startup.</param>
        /// <param name="requiredTransactionMode">The transaction mode required for receive operations.</param>
        public PushSettings(string inputQueue, bool purgeOnStartup, TransportTransactionMode requiredTransactionMode)
        {
            Guard.AgainstNullAndEmpty(nameof(inputQueue), inputQueue);
            Guard.AgainstNull(nameof(requiredTransactionMode), requiredTransactionMode);

            PurgeOnStartup = purgeOnStartup;
            RequiredTransactionMode = requiredTransactionMode;
            InputQueue = inputQueue;
        }

        /// <summary>
        /// The native queue to consume messages from.
        /// </summary>
        public string InputQueue { get; private set; }

        /// <summary>
        /// Instructs the message pump to purge the `InputQueue` before starting to push messages from it.
        /// </summary>
        public bool PurgeOnStartup { get; private set; }

        /// <summary>
        /// The transaction mode required for receive operations.
        /// </summary>
        public TransportTransactionMode RequiredTransactionMode { get; private set; }
    }
}