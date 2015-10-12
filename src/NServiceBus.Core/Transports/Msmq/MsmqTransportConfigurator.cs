namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Messaging;
    using System.Security;
    using System.Transactions;
    using Logging;
    using ObjectBuilder;
    using Settings;
    using Transports;
    using Transports.Msmq;
    using Transports.Msmq.Config;
    using Utils;
    using TransactionSettings = Unicast.Transport.TransactionSettings;

    /// <summary>
    /// Used to configure the MSMQ transport.
    /// </summary>
    public class MsmqTransportConfigurator : ConfigureTransport
    {
        internal MsmqTransportConfigurator()
        {
            DependsOn<UnicastBus>();
            RegisterStartupTask<CheckQueuePermissions>();
        }

        class CheckQueuePermissions : FeatureStartupTask
        {
            IBuilder builder;

            public CheckQueuePermissions(IBuilder builder)
            {
                this.builder = builder;
            }

            protected override void OnStart()
            {
                var settings = builder.Build<ReadOnlySettings>();
                var queueBindings = settings.Get<QueueBindings>();
                var boundQueueAddresses = queueBindings.ReceivingAddresses.Concat(queueBindings.SendingAddresses);

                foreach (var address in boundQueueAddresses)
                {
                    CheckQueue(address);
                }
            }

            static void CheckQueue(string address)
            {
                var msmqAddress = MsmqAddress.Parse(address);
                var queuePath = msmqAddress.PathWithoutPrefix;

                if (MessageQueue.Exists(queuePath))
                {
                    using (var messageQueue = new MessageQueue(queuePath))
                    {
                        WarnIfPublicAccess(messageQueue);
                    }
                }
            }

            static void WarnIfPublicAccess(MessageQueue queue)
            {
                MessageQueueAccessRights? everyoneRights, anonymousRights;

                try
                {
                    queue.TryGetPermissions(QueueCreator.LocalAnonymousLogonName, out anonymousRights);
                    queue.TryGetPermissions(QueueCreator.LocalEveryoneGroupName, out everyoneRights);
                }
                catch (SecurityException se)
                {
                    Logger.Warn($"Unable to read permissions for queue [{queue.QueueName}]. Make sure you have administrative access on the target machine", se);
                    return;
                }

                if (anonymousRights.HasValue && everyoneRights.HasValue)
                {
                    var logMessage = $"Queue [{queue.QueueName}] is running with [{QueueCreator.LocalEveryoneGroupName}] and [{QueueCreator.LocalAnonymousLogonName}] permissions. Consider setting appropriate permissions, if required by your organization. For more information, please consult the documentation.";

                    if (Debugger.IsAttached)
                        Logger.Info(logMessage);
                    else
                        Logger.Warn(logMessage);
                }
            }

            static ILog Logger = LogManager.GetLogger<CheckQueuePermissions>();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigureTransport"/>.
        /// </summary>
        protected override void Configure(FeatureConfigurationContext context, string connectionString)
        {
            new CheckMachineNameForComplianceWithDtcLimitation().Check();

            Func<IReadOnlyDictionary<string, string>, string> getMessageLabel;
            context.Settings.TryGet("Msmq.GetMessageLabel", out getMessageLabel);

            var settings = new MsmqSettings();
            if (connectionString != null)
            {
                settings = new MsmqConnectionStringBuilder(connectionString).RetrieveSettings();
            }

            var messageLabelGenerator = context.Settings.GetMessageLabelGenerator();
            context.Container.ConfigureComponent(b => new MsmqMessageSender(settings, messageLabelGenerator), DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent<QueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Settings, settings);


            if (!context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                var transactionSettings = new TransactionSettings(context.Settings);
                var transactionOptions = new TransactionOptions
                {
                    IsolationLevel = transactionSettings.IsolationLevel,
                    Timeout = transactionSettings.TransactionTimeout
                };

                context.Container.ConfigureComponent(b => new MessagePump(b.Build<CriticalError>(), guarantee => SelectReceiveStrategy(guarantee, transactionOptions)), DependencyLifecycle.InstancePerCall);
            }
        }

        /// <summary>
        /// <see cref="ConfigureTransport.ExampleConnectionStringForErrorMessage"/>.
        /// </summary>
        protected override string ExampleConnectionStringForErrorMessage => "cacheSendConnection=true;journal=false;deadLetter=true";

        /// <summary>
        /// <see cref="ConfigureTransport.RequiresConnectionString"/>.
        /// </summary>
        protected override bool RequiresConnectionString => false;


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


    }
}