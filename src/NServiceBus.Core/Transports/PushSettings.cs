namespace NServiceBus.Transports
{
    /// <summary>
    /// Contains information necessary to set up a message pump for receiving messages.
    /// </summary>
    public class PushSettings
    {
        /// <summary>
        /// Creates an instance of <see cref="PushSettings"/>.
        /// </summary>
        /// <param name="inputQueue">Input queue name.</param>
        /// <param name="errorQueue">Error queue name.</param>
        /// <param name="purgeOnStartup"><code>true</code> to purge <paramref name="inputQueue"/> at startup.</param>
        /// <param name="requiredTransactionSupport">The required transaction support required for the receive operations.</param>
        public PushSettings(string inputQueue, string errorQueue, bool purgeOnStartup, TransactionSupport requiredTransactionSupport)
        {
            Guard.AgainstNullAndEmpty("inputQueue", inputQueue);
            Guard.AgainstNullAndEmpty("errorQueue", errorQueue);
            Guard.AgainstNull("requiredTransactionSupport", requiredTransactionSupport);

            PurgeOnStartup = purgeOnStartup;
            RequiredTransactionSupport = requiredTransactionSupport;
            InputQueue = inputQueue;
            ErrorQueue = errorQueue;
        }

        /// <summary>
        /// The native queue to consume messages from.
        /// </summary>
        public string InputQueue{ get; private set; }

        /// <summary>
        /// The native queue where to send corrupted messages to.
        /// </summary>
        public string ErrorQueue { get; private set; }

        /// <summary>
        /// Instructs the message pump to purge the `InputQueue` before starting to push messages from it.
        /// </summary>
        public bool PurgeOnStartup { get; private set; }

        /// <summary>
        /// The required transaction support required for the receive operations.
        /// </summary>
        public TransactionSupport RequiredTransactionSupport { get; private set; }
    }
}