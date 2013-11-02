namespace NServiceBus.Settings
{
    using System;
    using System.Configuration;
    using System.Transactions;
    using System.Transactions.Configuration;
    using Unicast.Transport;

    class DefaultTransactionSettings : ISetDefaultSettings
    {
        public DefaultTransactionSettings()
        {
            SettingsHolder.SetDefault("Transactions.Enabled", true);
        }
    }

    class DefaultTransactionAdvancedSettings : ISetDefaultSettings
    {
        public DefaultTransactionAdvancedSettings()
        {
            SettingsHolder.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
            SettingsHolder.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);
            SettingsHolder.SetDefault("Transactions.SuppressDistributedTransactions", false);
            SettingsHolder.SetDefault("Transactions.DoNotWrapHandlersExecutionInATransactionScope", false);
        }
    }

    /// <summary>
    ///     Configuration class for Transaction settings.
    /// </summary>
    public class TransactionSettings
    {
        private readonly TransactionAdvancedSettings transactionAdvancedSettings = new TransactionAdvancedSettings();

        /// <summary>
        ///     Configures this endpoint to use transactions.
        /// </summary>
        /// <remarks>
        ///     A transactional endpoint means that we don't remove a message from the queue until it has been successfully processed.
        /// </remarks>
        public TransactionSettings Enable()
        {
            SettingsHolder.Set("Transactions.Enabled", true);
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
            SettingsHolder.Set("Transactions.Enabled", false);
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
            private readonly TimeSpan maxTimeout;

            /// <summary>
            ///     Default constructor.
            /// </summary>
            public TransactionAdvancedSettings()
            {
                maxTimeout = GetMaxTimeout();
            }

            /// <summary>
            ///    Sets the isolation level of the transaction.
            /// </summary>
            /// <param name="isolationLevel">A <see cref="T:System.Transactions.IsolationLevel" /> enumeration that specifies the isolation level of the transaction.</param>
            public TransactionAdvancedSettings IsolationLevel(IsolationLevel isolationLevel)
            {
                SettingsHolder.Set("Transactions.IsolationLevel", isolationLevel);

                return this;
            }

            /// <summary>
            /// Configures the <see cref="ITransport" /> not to enlist in Distributed Transactions.
            /// </summary>
            public TransactionAdvancedSettings DisableDistributedTransactions()
            {
                SettingsHolder.Set("Transactions.SuppressDistributedTransactions", true);
                return this;
            }

            /// <summary>
            /// Configures the <see cref="ITransport" /> to enlist in Distributed Transactions.
            /// </summary>
            public TransactionAdvancedSettings EnableDistributedTransactions()
            {
                SettingsHolder.Set("Transactions.SuppressDistributedTransactions", false);
                return this;
            }

            /// <summary>
            /// Configures this endpoint so that <see cref="IHandleMessages{T}">handlers</see> are not wrapped in a <see cref="TransactionScope" />.
            /// </summary>
            public TransactionAdvancedSettings DoNotWrapHandlersExecutionInATransactionScope()
            {
                SettingsHolder.Set("Transactions.DoNotWrapHandlersExecutionInATransactionScope", true);
                return this;
            }

            /// <summary>
            /// Configures this endpoint so that <see cref="IHandleMessages{T}">handlers</see> not wrapped in a <see cref="TransactionScope" />.
            /// </summary>
            public TransactionAdvancedSettings WrapHandlersExecutionInATransactionScope()
            {
                SettingsHolder.Set("Transactions.DoNotWrapHandlersExecutionInATransactionScope", false);
                return this;
            }

            /// <summary>
            /// Sets the default timeout period for the transaction.
            /// </summary>
            /// <param name="defaultTimeout">A <see cref="T:System.TimeSpan" /> value that specifies the default timeout period for the transaction.</param>
            public TransactionAdvancedSettings DefaultTimeout(TimeSpan defaultTimeout)
            {
                if (defaultTimeout > maxTimeout)
                {
                    throw new ConfigurationErrorsException(
                        "Timeout requested is longer than the maximum value for this machine. Please override using the maxTimeout setting of the system.transactions section in machine.config");
                }

                SettingsHolder.Set("Transactions.DefaultTimeout", defaultTimeout);
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