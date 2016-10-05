namespace NServiceBus.Features
{
    using System;
    using System.Configuration;
    using System.Transactions;
    using System.Transactions.Configuration;
    using ConsistencyGuarantees;

    class TransactionScopeUnitOfWork : Feature
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.GetRequiredTransactionModeForReceives() == TransportTransactionMode.TransactionScope)
            {
                throw new Exception("A Transaction scope unit of work can't be used when the transport already uses a scope for the receive operation. Remove the call to config.UnitOfWork().WrapHandlersInATransactionScope() or configure the transport to use a lower transaction mode");
            }

            var transactionOptions = context.Settings.Get<Settings>().TransactionOptions;
            context.Pipeline.Register("HandlerTransactionScopeWrapper", new TransactionScopeUnitOfWorkBehavior(transactionOptions), "Makes sure that the handlers gets wrapped in a transaction scope");
        }

        public class Settings
        {
            public Settings(TimeSpan? requestedTimeout, IsolationLevel? requestedIsolationLevel)
            {
                var timeout = TransactionManager.DefaultTimeout;
                var isolationLevel = IsolationLevel.ReadCommitted;
                if (requestedTimeout.HasValue)
                {
                    var maxTimeout = GetMaxTimeout();

                    if (requestedTimeout.Value > maxTimeout)
                    {
                        throw new ConfigurationErrorsException(
                            "Timeout requested is longer than the maximum value for this machine. Override using the maxTimeout setting of the system.transactions section in machine.config");
                    }

                    timeout = requestedTimeout.Value;
                }

                if (requestedIsolationLevel.HasValue)
                {
                    isolationLevel = requestedIsolationLevel.Value;
                }

                TransactionOptions = new TransactionOptions
                {
                    IsolationLevel = isolationLevel,
                    Timeout = timeout
                };
            }

            public TransactionOptions TransactionOptions { get; }

            static TimeSpan GetMaxTimeout()
            {
                //default is always 10 minutes
                var maxTimeout = TimeSpan.FromMinutes(10);

                var systemTransactionsGroup = ConfigurationManager.OpenMachineConfiguration()
                    .GetSectionGroup("system.transactions");

                var machineSettings = systemTransactionsGroup?.Sections.Get("machineSettings") as MachineSettingsSection;

                if (machineSettings != null)
                {
                    maxTimeout = machineSettings.MaxTimeout;
                }

                return maxTimeout;
            }
        }
    }
}