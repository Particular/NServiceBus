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
    using Support;
    using Transport;

    class MsmqTransportInfrastructure : TransportInfrastructure
    {
        public MsmqTransportInfrastructure(ReadOnlySettings settings, string connectionString)
        {
            RequireOutboxConsent = true;

            this.settings = settings;
            this.connectionString = connectionString;
            this.addressTranslationRule = settings.GetOrDefault<Func<EndpointInstance, string>>("NServiceBus.Transports.MSMQ.AddressTranslationRule") ?? DefaultAddressTranslationRule;
        }

        public override IEnumerable<Type> DeliveryConstraints { get; } = new[]
        {
            typeof(DiscardIfNotReceivedBefore),
            typeof(NonDurableDelivery)
        };

        public override TransportTransactionMode TransactionMode { get; } = TransportTransactionMode.TransactionScope;
        public override OutboundRoutingPolicy OutboundRoutingPolicy { get; } = new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast);

        ReceiveStrategy SelectReceiveStrategy(TransportTransactionMode minimumConsistencyGuarantee, TransactionOptions transactionOptions)
        {
            if (minimumConsistencyGuarantee == TransportTransactionMode.TransactionScope)
            {
                return new ReceiveWithTransactionScope(transactionOptions, new MsmqFailureInfoStorage(1000));
            }

            if (minimumConsistencyGuarantee == TransportTransactionMode.None)
            {
                return new ReceiveWithNoTransaction();
            }

            return new ReceiveWithNativeTransaction(new MsmqFailureInfoStorage(1000));
        }

        public override string ToTransportAddress(LocalAddress localAddress)
        {
            var address = new StringBuilder(localAddress.InstanceName);

            if (localAddress.Discriminator != null)
            {
                address.Append("-" + localAddress.Discriminator);
            }

            if (localAddress.Qualifier != null)
            {
                address.Append("." + localAddress.Qualifier);
            }

            return address + "@" + RuntimeEnvironment.MachineName;
        }

        public override string ToTransportAddress(EndpointInstance endpointInstance)
        {
            return addressTranslationRule(endpointInstance);
        }

        static string DefaultAddressTranslationRule(EndpointInstance endpointInstance)
        {
            var address = new StringBuilder(endpointInstance.Endpoint);

            if (endpointInstance.Discriminator != null)
            {
                address.Append("-" + endpointInstance.Discriminator);
            }

            string machineName;
            if (!endpointInstance.Properties.TryGetValue("machine", out machineName))
            {
                machineName = RuntimeEnvironment.MachineName;
            }

            address.Append("@").Append(machineName);
            return address.ToString();
        }

        public override string MakeCanonicalForm(string transportAddress)
        {
            return MsmqAddress.Parse(transportAddress).ToString();
        }

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

                    foreach (var address in bindings.ReceivingAddresses)
                    {
                        QueuePermissions.CheckQueue(address);
                    }
                    return Task.FromResult(StartupCheckResult.Success);
                });
        }

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

                    foreach (var address in bindings.SendingAddresses)
                    {
                        QueuePermissions.CheckQueue(address);
                    }

                    var result = new MsmqTimeToBeReceivedOverrideCheck(settings).CheckTimeToBeReceivedOverrides();
                    return Task.FromResult(result);
                });
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            throw new NotImplementedException("MSMQ does not support native pub/sub.");
        }

        string connectionString;
        ReadOnlySettings settings;
        Func<EndpointInstance, string> addressTranslationRule;
    }
}