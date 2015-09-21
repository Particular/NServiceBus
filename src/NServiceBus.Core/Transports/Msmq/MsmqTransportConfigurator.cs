namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Messaging;
    using System.Security;
    using System.Transactions;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Transports.Msmq;
    using NServiceBus.Transports.Msmq.Config;
    using NServiceBus.Utils;
    using TransactionSettings = NServiceBus.Unicast.Transport.TransactionSettings;

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
                    Logger.Warn(string.Format("Unable to read permissions for queue [{0}]. Make sure you have administrative access on the target machine", queue.QueueName), se);
                    return;
                }

                if (anonymousRights.HasValue && everyoneRights.HasValue)
                {
                    var logMessage = string.Format("Queue [{0}] is running with [{1}] and [{2}] permissions. Consider setting appropriate permissions, if required by your organization. For more information, please consult the documentation.",
                        queue.QueueName,
                        QueueCreator.LocalEveryoneGroupName,
                        QueueCreator.LocalAnonymousLogonName);

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


            if (context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                return;
            }

            var transactionSettings = new TransactionSettings(context.Settings);

            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = transactionSettings.IsolationLevel,
                Timeout = transactionSettings.TransactionTimeout
            };

            context.Container.ConfigureComponent(b => new MessagePump(b.Build<CriticalError>(), guarantee => SelectReceiveStrategy(guarantee, transactionOptions)), DependencyLifecycle.InstancePerCall);
        }

        /// <summary>
        /// <see cref="ConfigureTransport.ExampleConnectionStringForErrorMessage"/>.
        /// </summary>
        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "cacheSendConnection=true;journal=false;deadLetter=true"; }
        }

        /// <summary>
        /// <see cref="ConfigureTransport.RequiresConnectionString"/>.
        /// </summary>
        protected override bool RequiresConnectionString
        {
            get { return false; }
        }


        ReceiveStrategy SelectReceiveStrategy(ConsistencyGuarantee minimumConsistencyGuarantee, TransactionOptions transactionOptions)
        {
            if (minimumConsistencyGuarantee == ConsistencyGuarantee.ExactlyOnce)
            {
                return new ReceiveWithTransactionScope(transactionOptions);
            }

            if (minimumConsistencyGuarantee == ConsistencyGuarantee.AtMostOnce)
            {
                return new ReceiveWithNoTransaction();
            }

            return new ReceiveWithNativeTransaction();
        }


    }
}