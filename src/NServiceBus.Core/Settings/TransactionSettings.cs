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
        ConfigurationBuilder config;
        TransactionAdvancedSettings transactionAdvancedSettings;

        internal TransactionSettings(ConfigurationBuilder config)
        {
            this.config = config;
            transactionAdvancedSettings = new TransactionAdvancedSettings(config);
        }

        /// <summary>
        ///     Configures this endpoint to use transactions.
        /// </summary>
        /// <remarks>
        ///     A transactional endpoint means that we don't remove a message from the queue until it has been successfully processed.
        /// </remarks>
        public TransactionSettings Enable()
        {
            config.settings.Set("Transactions.Enabled", true);
            return this;
        }

        /// <summary>
        ///     Configures this endpoint to not use transactions.
        /// </summary>
        /// <remarks>
        ///     Turning transactions off means that the endpoint won't do retries and messages are lost on exceptions.
        /// </remarks>
        public TransactionSettings Disable()
        {
            config.settings.Set("Transactions.Enabled", false);
            config.settings.SetDefault("Transactions.DoNotWrapHandlersExecutionInATransactionScope", false);
            return this;
        }

        /// <summary>
        ///     <see cref="TransactionSettings" /> advance settings.
        /// </summary>
        /// <param name="action">A lambda to set the advance settings.</param>
        public TransactionSettings Advanced(Action<TransactionAdvancedSettings> action)
        {
            action(transactionAdvancedSettings);
            return this;
        }

        /// <summary>
        ///     <see cref="TransactionSettings" /> advance settings.
        /// </summary>
        public class TransactionAdvancedSettings
        {
            ConfigurationBuilder config;
            TimeSpan maxTimeout;

            /// <summary>
            ///     Default constructor.
            /// </summary>
            public TransactionAdvancedSettings(ConfigurationBuilder config)
            {
                this.config = config;
                maxTimeout = GetMaxTimeout();
            }

            /// <summary>
            ///    Sets the isolation level of the transaction.
            /// </summary>
            /// <param name="isolationLevel">A <see cref="IsolationLevel" /> enumeration that specifies the isolation level of the transaction.</param>
            public TransactionAdvancedSettings IsolationLevel(IsolationLevel isolationLevel)
            {
                config.settings.Set("Transactions.IsolationLevel", isolationLevel);

                return this;
            }

            /// <summary>
            /// Configures the <see cref="ITransport" /> not to enlist in Distributed Transactions.
            /// </summary>
            public TransactionAdvancedSettings DisableDistributedTransactions()
            {
                config.settings.Set("Transactions.SuppressDistributedTransactions", true);
                config.settings.SetDefault("Transactions.DoNotWrapHandlersExecutionInATransactionScope", false);
                return this;
            }

            /// <summary>
            /// Configures the <see cref="ITransport" /> to enlist in Distributed Transactions.
            /// </summary>
            public TransactionAdvancedSettings EnableDistributedTransactions()
            {
                config.settings.Set("Transactions.SuppressDistributedTransactions", false);
                return this;
            }

            /// <summary>
            /// Configures this endpoint so that <see cref="IHandleMessages{T}">handlers</see> are not wrapped in a <see cref="TransactionScope" />.
            /// </summary>
            public TransactionAdvancedSettings DoNotWrapHandlersExecutionInATransactionScope()
            {
                config.settings.Set("Transactions.DoNotWrapHandlersExecutionInATransactionScope", true);
                return this;
            }

            /// <summary>
            /// Configures this endpoint so that <see cref="IHandleMessages{T}">handlers</see> not wrapped in a <see cref="TransactionScope" />.
            /// </summary>
            public TransactionAdvancedSettings WrapHandlersExecutionInATransactionScope()
            {
                config.settings.Set("Transactions.DoNotWrapHandlersExecutionInATransactionScope", false);
                return this;
            }

            /// <summary>
            /// Sets the default timeout period for the transaction.
            /// </summary>
            /// <param name="defaultTimeout">A <see cref="TimeSpan" /> value that specifies the default timeout period for the transaction.</param>
            public TransactionAdvancedSettings DefaultTimeout(TimeSpan defaultTimeout)
            {
                if (defaultTimeout > maxTimeout)
                {
                    throw new ConfigurationErrorsException(
                        "Timeout requested is longer than the maximum value for this machine. Please override using the maxTimeout setting of the system.transactions section in machine.config");
                }

                config.settings.Set("Transactions.DefaultTimeout", defaultTimeout);
                return this;
            }

            private static TimeSpan GetMaxTimeout()
            {
                //default is 10 always 10 minutes
                var maxTimeout = TimeSpan.FromMinutes(10);

                var systemTransactionsGroup = ConfigurationManager.OpenMachineConfiguration()
                                                                  .GetSectionGroup("system.transactions");

                if (systemTransactionsGroup != null)
                {
                    var machineSettings = systemTransactionsGroup.Sections.Get("machineSettings") as MachineSettingsSection;

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