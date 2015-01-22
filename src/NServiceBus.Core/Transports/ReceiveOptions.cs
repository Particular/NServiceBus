namespace NServiceBus.Transports
{
    using NServiceBus.Faults;
    using NServiceBus.Settings;
    using TransactionSettings = NServiceBus.Unicast.Transport.TransactionSettings;

    /// <summary>
    /// Contains confguration for creating a receive behavior.
    /// </summary>
    public class ReceiveOptions
    {
        readonly string errorQueue;
        readonly TransactionSettings transactions;
        readonly ReadOnlySettings settings;

        internal ReceiveOptions(ReadOnlySettings settings)
        {
            this.settings = settings;
            errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(Settings).ToString();
            transactions = new TransactionSettings(Settings);
        }

        /// <summary>
        /// Gets the error queue name.
        /// </summary>
        public string ErrorQueue
        {
            get { return errorQueue; }
        }

        /// <summary>
        /// Gets the trnsaction options.
        /// </summary>
        public TransactionSettings Transactions
        {
            get { return transactions; }
        }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        public ReadOnlySettings Settings
        {
            get { return settings; }
        }
    }
}