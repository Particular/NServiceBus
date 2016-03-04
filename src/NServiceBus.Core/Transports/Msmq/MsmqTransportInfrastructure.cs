namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using System.Transactions;
    using Features;
    using Performance.TimeToBeReceived;
    using Routing;
    using Settings;
    using Transports;

    /// <summary>
    /// Transport infrastructure for MSMQ.
    /// </summary>
    public class MsmqTransportInfrastructure : TransportInfrastructure
    {
        internal MsmqTransportInfrastructure(ReadOnlySettings settings, string connectionString)
        {
            RequireOutboxConsent = true;

            this.settings = settings;
            this.connectionString = connectionString;
        }

        /// <summary>
        /// <see cref="TransportInfrastructure.DeliveryConstraints" />.
        /// </summary>
        public override IEnumerable<Type> DeliveryConstraints { get; } = new[]
        {
            typeof(DiscardIfNotReceivedBefore)
        };

        /// <summary>
        /// <see cref="TransportInfrastructure.TransactionMode" />.
        /// </summary>
        public override TransportTransactionMode TransactionMode { get; } = TransportTransactionMode.TransactionScope;

        /// <summary>
        /// <see cref="TransportInfrastructure.OutboundRoutingPolicy" />.
        /// </summary>
        public override OutboundRoutingPolicy OutboundRoutingPolicy { get; } = new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast);

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

        /// <summary>
        /// Returns the discriminator for this endpoint instance.
        /// </summary>
        public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance) => instance.AtMachine(Environment.MachineName);

        /// <summary>
        /// <see cref="TransportInfrastructure.ToTransportAddress" />.
        /// </summary>
        /// <param name="logicalAddress">The logical address.</param>
        /// <returns>The transport address.</returns>
        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            string machine;
            if (!logicalAddress.EndpointInstance.Properties.TryGetValue("Machine", out machine))
            {
                machine = Environment.MachineName;
            }

            var queue = new StringBuilder(logicalAddress.EndpointInstance.Endpoint.ToString());
            if (logicalAddress.EndpointInstance.Discriminator != null)
            {
                queue.Append("-" + logicalAddress.EndpointInstance.Discriminator);
            }
            if (logicalAddress.Qualifier != null)
            {
                queue.Append("." + logicalAddress.Qualifier);
            }
            return queue + "@" + machine;
        }

        /// <summary>
        /// <see cref="TransportInfrastructure.MakeCanonicalForm" />.
        /// </summary>
        /// <param name="transportAddress">A transport address.</param>
        public override string MakeCanonicalForm(string transportAddress)
        {
            return MsmqAddress.Parse(transportAddress).ToString();
        }

        /// <summary>
        /// <see cref="TransportInfrastructure.ConfigureReceiveInfrastructure" />.
        /// </summary>
        /// <returns>Transport receive infrastructure.</returns>
        public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
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
        }

        /// <summary>
        /// <see cref="TransportInfrastructure.ConfigureSendInfrastructure" />.
        /// </summary>
        /// <returns>Transport send infrastructure.</returns>
        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {
            new CheckMachineNameForComplianceWithDtcLimitation().Check();

            Func<IReadOnlyDictionary<string, string>, string> getMessageLabel;
            settings.TryGet("Msmq.GetMessageLabel", out getMessageLabel);

            Func<IReadOnlyDictionary<string, string>, string> messageLabelGenerator;
            if (!settings.TryGet("msmqLabelGenerator", out messageLabelGenerator))
            {
                messageLabelGenerator = headers => string.Empty;
            }

            var builder = new MsmqConnectionStringBuilder(connectionString).RetrieveSettings();

            return new TransportSendInfrastructure(
                () => new MsmqMessageDispatcher(builder, messageLabelGenerator),
                () =>
                {
                    var bindings = settings.Get<QueueBindings>();
                    new QueuePermissionChecker().CheckQueuePermissions(bindings.SendingAddresses);
                    var result = new MsmqTimeToBeReceivedOverrideCheck(settings).CheckTimeToBeReceivedOverrides();
                    return Task.FromResult(result);
                });
        }

        /// <summary>
        /// <see cref="TransportInfrastructure.ConfigureSubscriptionInfrastructure" />.
        /// </summary>
        /// <returns>Transport subscription infrastructure.</returns>
        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            throw new NotImplementedException("MSMQ does not support native pub/sub.");
        }

        readonly string connectionString;

        readonly ReadOnlySettings settings;
    }
}