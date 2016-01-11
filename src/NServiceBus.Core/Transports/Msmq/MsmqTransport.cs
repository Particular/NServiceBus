namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Threading.Tasks;
    using System.Transactions;
    using System.Transactions.Configuration;
    using NServiceBus.Features;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    /// <summary>
    /// Transport definition for MSMQ.
    /// </summary>
    public class MsmqTransport : TransportDefinition
    {
        /// <summary>
        /// Initializes the transport infrastructure for msmq.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns>the transport infrastructure for msmq.</returns>
        protected internal override TransportInfrastructure Initialize(SettingsHolder settings)
        {
            return new MsmqTransportInfrastructure(
                new[] {
                    typeof(DiscardIfNotReceivedBefore)
                },
                TransportTransactionMode.TransactionScope,
                new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast),
                connectionString =>
                {
                    new CheckMachineNameForComplianceWithDtcLimitation().Check();

                    Func<IReadOnlyDictionary<string, string>, string> getMessageLabel;
                    settings.TryGet("Msmq.GetMessageLabel", out getMessageLabel);
                    var builder = new MsmqConnectionStringBuilder(connectionString).RetrieveSettings();

                    Func<IReadOnlyDictionary<string, string>, string> messageLabelGenerator;
                    if (!settings.TryGet("msmqLabelGenerator", out messageLabelGenerator))
                    {
                        messageLabelGenerator = headers => string.Empty;
                    }
                    return new TransportSendInfrastructure(
                        () => new MsmqMessageDispatcher(builder, messageLabelGenerator),
                        () =>
                        {
                            var bindings = settings.Get<QueueBindings>();
                            new QueuePermissionChecker().CheckQueuePermissions(bindings.SendingAddresses);
                            var result = new MsmqTimeToBeReceivedOverrideCheck(settings).CheckTimeToBeReceivedOverrides();
                            return Task.FromResult(result);
                        });
                }, connectionString =>
                {
                    new CheckMachineNameForComplianceWithDtcLimitation().Check();

                    var builder = connectionString != null
                        ? new MsmqConnectionStringBuilder(connectionString).RetrieveSettings()
                        : new MsmqSettings();

                    MsmqScopeOptions scopeOptions;

                    if (!settings.TryGet(out scopeOptions))
                    {
                        scopeOptions = new MsmqScopeOptions();
                    }

                    return new TransportReceiveInfrastructure(
                        () => new MessagePump(guarantee => SelectReceiveStrategy(guarantee, scopeOptions.TransactionOptions)),
                        () => new QueueCreator(builder),
                        () =>
                        {
                            var bindings = settings.Get<QueueBindings>();
                            new QueuePermissionChecker().CheckQueuePermissions(bindings.ReceivingAddresses);
                            return Task.FromResult(StartupCheckResult.Success);
                        });
                });

        }

        ReceiveStrategy SelectReceiveStrategy(TransportTransactionMode minimumConsistencyGuarantee, TransactionOptions transactionOptions)
        {
            if (minimumConsistencyGuarantee == TransportTransactionMode.TransactionScope)
            {
                return new ReceiveWithTransactionScope(transactionOptions);
            }

            if (minimumConsistencyGuarantee == TransportTransactionMode.None)
            {
                return new ReceiveWithNoTransaction();
            }

            return new ReceiveWithNativeTransaction();
        }

        internal class MsmqScopeOptions
        {
            public TransactionOptions TransactionOptions { get; }

            public MsmqScopeOptions(TimeSpan? requestedTimeout = null, IsolationLevel? requestedIsolationLevel = null)
            {
                var timeout = TransactionManager.DefaultTimeout;
                var isolationLevel = IsolationLevel.ReadCommitted;
                if (requestedTimeout.HasValue)
                {
                    var maxTimeout = GetMaxTimeout();

                    if (requestedTimeout.Value > maxTimeout)
                    {
                        throw new ConfigurationErrorsException(
                            "Timeout requested is longer than the maximum value for this machine. Please override using the maxTimeout setting of the system.transactions section in machine.config");
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