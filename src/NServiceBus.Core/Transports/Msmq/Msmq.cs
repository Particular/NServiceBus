namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Features;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Settings;
    using NServiceBus.Support;
    using NServiceBus.Transports;
    using NServiceBus.Transports.Msmq;
    using NServiceBus.Transports.Msmq.Config;
    using TransactionSettings = NServiceBus.Unicast.Transport.TransactionSettings;

    /// <summary>
    /// Transport definition for MSMQ.
    /// </summary>
    public class MsmqTransport : TransportDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MsmqTransport"/>.
        /// </summary>
        public MsmqTransport()
        {
            RequireOutboxConsent = true;
        }

        /// <summary>
        /// Configures transport for receiving.
        /// </summary>
        protected internal override TransportReceivingConfigurationResult ConfigureForReceiving(TransportReceivingConfigurationContext context)
        {
            new CheckMachineNameForComplianceWithDtcLimitation().Check();

            var settings = context.ConnectionString != null
                ? new MsmqConnectionStringBuilder(context.ConnectionString).RetrieveSettings()
                : new MsmqSettings();

            var transactionSettings = new TransactionSettings(context.Settings);
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = transactionSettings.IsolationLevel,
                Timeout = transactionSettings.TransactionTimeout
            };

            return new TransportReceivingConfigurationResult(
                () => new MessagePump(guarantee => SelectReceiveStrategy(guarantee, transactionOptions)),
                () => new QueueCreator(settings), 
                () =>
                {
                    var bindings = context.Settings.Get<QueueBindings>();
                    new QueuePermissionChecker().CheckQueuePermissions(bindings.ReceivingAddresses);
                    return Task.FromResult(StartupCheckResult.Success);
                });
        }

        ReceiveStrategy SelectReceiveStrategy(TransactionSupport minimumConsistencyGuarantee, TransactionOptions transactionOptions)
        {
            if (minimumConsistencyGuarantee == TransactionSupport.Distributed)
            {
                return new ReceiveWithTransactionScope(transactionOptions);
            }

            if (minimumConsistencyGuarantee == TransactionSupport.None)
            {
                return new ReceiveWithNoTransaction();
            }

            return new ReceiveWithNativeTransaction();
        }
        
        /// <summary>
        /// Configures transport for sending.
        /// </summary>
        protected internal override TransportSendingConfigurationResult ConfigureForSending(TransportSendingConfigurationContext context)
        {
            new CheckMachineNameForComplianceWithDtcLimitation().Check();

            Func<IReadOnlyDictionary<string, string>, string> getMessageLabel;
            context.Settings.TryGet("Msmq.GetMessageLabel", out getMessageLabel);
            var settings = new MsmqConnectionStringBuilder(context.ConnectionString).RetrieveSettings();

            MsmqLabelGenerator messageLabelGenerator;
            if (!context.Settings.TryGet(out messageLabelGenerator))
            {
                messageLabelGenerator = headers => string.Empty;
            }
            return new TransportSendingConfigurationResult(
                () => new MsmqMessageSender(settings, messageLabelGenerator),
                () =>
                {
                    var bindings = context.Settings.Get<QueueBindings>();
                    new QueuePermissionChecker().CheckQueuePermissions(bindings.SendingAddresses);
                    var result = new MsmqTimeToBeReceivedOverrideCheck(context.Settings).CheckTimeToBeReceivedOverrides();
                    return Task.FromResult(result);
                });
        }

        /// <summary>
        /// The list of constraints supported by the MSMQ transport.
        /// </summary>
        public override IEnumerable<Type> GetSupportedDeliveryConstraints()
        {
            return new[]
            {
                typeof(DiscardIfNotReceivedBefore)
            };
        }

        /// <summary>
        /// Gets the supported transactionality for this transport.
        /// </summary>
        public override TransactionSupport GetTransactionSupport()
        {
            return TransactionSupport.Distributed;
        }

        /// <summary>
        /// Not used by the msmq transport.
        /// </summary>
        public override IManageSubscriptions GetSubscriptionManager()
        {
            throw new NotSupportedException("Msmq don't support native pub sub");
        }

        /// <summary>
        /// Returns the discriminator for this endpoint instance.
        /// </summary>
        public override string GetDiscriminatorForThisEndpointInstance(ReadOnlySettings settings)
        {
            return RuntimeEnvironment.MachineName;
        }
       
        /// <summary>
        /// Converts a given logical address to the transport address.
        /// </summary>
        /// <param name="logicalAddress">The logical address.</param>
        /// <returns>The transport address.</returns>
        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            var machine = logicalAddress.EndpointInstance.TransportDiscriminator ?? RuntimeEnvironment.MachineName;

            var queue = new StringBuilder(logicalAddress.EndpointInstance.Endpoint.ToString());
            if (logicalAddress.EndpointInstance.UserDiscriminator != null)
            {
                queue.Append("-" + logicalAddress.EndpointInstance.UserDiscriminator);
            }
            if (logicalAddress.Qualifier != null)
            {
                queue.Append("." + logicalAddress.Qualifier);
            }
            return queue + "@" + machine;
        }

        /// <summary>
        /// Returns the outbound routing policy selected for the transport.
        /// </summary>
        public override OutboundRoutingPolicy GetOutboundRoutingPolicy(ReadOnlySettings settings)
        {
            return new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast);
        }

        /// <summary>
        /// Gets an example connection string to use when reporting lack of configured connection string to the user.
        /// </summary>
        public override string ExampleConnectionStringForErrorMessage => "cacheSendConnection=true;journal=false;deadLetter=true";

        /// <summary>
        /// Used by implementations to control if a connection string is necessary.
        /// </summary>
        public override bool RequiresConnectionString => false;
    }
}