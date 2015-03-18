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
        internal ReceiveOptions(ReadOnlySettings settings)
        {
            Settings = settings;
            ErrorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(Settings);
            Transactions = new TransactionSettings(Settings);
        }

        /// <summary>
        /// Gets the error queue name.
        /// </summary>
        public string ErrorQueue { get; private set; }

        /// <summary>
        /// Gets the trnsaction options.
        /// </summary>
        public TransactionSettings Transactions { get; private set; }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        public ReadOnlySettings Settings { get; private set; }
    }
}