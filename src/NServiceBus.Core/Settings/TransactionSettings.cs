namespace NServiceBus.Settings
{
    using System;
    using System.Configuration;
    using System.Transactions;
    using System.Transactions.Configuration;
    using Unicast.Transport;

    /// <summary>
    ///     Configuration class for Transaction settings.
    /// </summary>
    public class TransactionSettings
    {
        private readonly TransactionAdvancedSettings transactionAdvancedSettings = new TransactionAdvancedSettings();

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public TransactionSettings()
        {
            Enabled = true;
        }

        /// <summary>
        ///     Returns <code>true</code> is transactions are enabled for this endpoint.
        /// </summary>
        public bool Enabled { get; private set; }

        /// <summary>
        ///     Configures this endpoint to use transactions.
        /// </summary>
        /// <remarks>
        ///     A transactional endpoint means that we don't remove a message from the queue until it has been successfully processed.
        /// </remarks>
        public void Enable()
        {
            Enabled = true;
        }

        /// <summary>
        ///     Configures this endpoint to not use transactions.
        /// </summary>
        /// <remarks>
        ///     Turning transactions off means that the endpoint won't do retries and messages are lost on exceptions.
        /// </remarks>
        public void Disable()
        {
            Enabled = false;
        }

        /// <summary>
        ///     <see cref="TransactionSettings" /> advance settings.
        /// </summary>
        /// <returns>The advance settings.</returns>
        public TransactionAdvancedSettings Advanced()
        {
            return transactionAdvancedSettings;
        }

        /// <summary>
        ///     <see cref="TransactionSettings" /> advance settings.
        /// </summary>
        /// <param name="action">A lambda to set the advance settings.</param>
        public void Advanced(Action<TransactionAdvancedSettings> action)
        {
            action(transactionAdvancedSettings);
        }

        /// <summary>
        ///     <see cref="TransactionSettings" /> advance settings.
        /// </summary>
        public class TransactionAdvancedSettings
        {
            private TimeSpan defaultTimeout;

            /// <summary>
            ///     Default constructor.
            /// </summary>
            public TransactionAdvancedSettings()
            {
                IsolationLevel = IsolationLevel.ReadCommitted;
                DefaultTimeout = TransactionManager.DefaultTimeout;
            }

            /// <summary>
            ///     Gets or sets the isolation level of the transaction.
            /// </summary>
            /// <returns>
            ///     A <see cref="T:System.Transactions.IsolationLevel" /> enumeration that specifies the isolation level of the transaction.
            /// </returns>
            public IsolationLevel IsolationLevel { get; set; }

            /// <summary>
            ///     Configures the <see cref="ITransport" /> not to enlist in Distributed Transactions.
            /// </summary>
            public bool SuppressDistributedTransactions { get; set; }

            /// <summary>
            ///     Configures this endpoint so that <see cref="IHandleMessages{T}">handlers</see> are not wrapped in a
            ///     <see
            ///         cref="TransactionScope" />
            ///     .
            /// </summary>
            public bool DoNotWrapHandlersExecutionInATransactionScope { get; set; }

            /// <summary>
            ///     Gets or sets the default timeout period for the transaction.
            /// </summary>
            /// <returns>
            ///     A <see cref="T:System.TimeSpan" /> value that specifies the default timeout period for the transaction.
            /// </returns>
            public TimeSpan DefaultTimeout
            {
                get { return defaultTimeout; }
                set
                {
                    TimeSpan maxTimeout = GetMaxTimeout();

                    if (value > maxTimeout)
                        throw new ConfigurationErrorsException(
                            "Timeout requested is longer than the maximum value for this machine. Please override using the maxTimeout setting of the system.transactions section in machine.config");

                    defaultTimeout = value;
                }
            }

            private static TimeSpan GetMaxTimeout()
            {
                //default is 10 always 10 minutes
                TimeSpan maxTimeout = TimeSpan.FromMinutes(10);

                ConfigurationSectionGroup systemTransactionsGroup = ConfigurationManager.OpenMachineConfiguration()
                                                                                        .GetSectionGroup(
                                                                                            "system.transactions");

                if (systemTransactionsGroup != null)
                {
                    var machineSettings =
                        systemTransactionsGroup.Sections.Get("machineSettings") as MachineSettingsSection;

                    if (machineSettings != null)
                    {
                        maxTimeout = machineSettings.MaxTimeout;
                    }
                }

                return maxTimeout;
            }
        }
    }
}