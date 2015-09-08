﻿namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Messaging;
    using System.Security;
    using System.Transactions;
    using NServiceBus.Config;
    using Logging;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Transports.Msmq;
    using NServiceBus.Transports.Msmq.Config;
    using NServiceBus.Utils;

    /// <summary>
    ///     Used to configure the MSMQ transport.
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
                var queuePath = MsmqQueueCreator.GetFullPathWithoutPrefix(msmqAddress);

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
                    queue.TryGetPermissions(MsmqQueueCreator.LocalAnonymousLogonName, out anonymousRights);
                    queue.TryGetPermissions(MsmqQueueCreator.LocalEveryoneGroupName, out everyoneRights);
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
                        MsmqQueueCreator.LocalEveryoneGroupName,
                        MsmqQueueCreator.LocalAnonymousLogonName);

                    if (Debugger.IsAttached)
                        Logger.Info(logMessage);
                    else
                        Logger.Warn(logMessage);
                }
            }

            static ILog Logger = LogManager.GetLogger<CheckQueuePermissions>();
        }

        /// <summary>
        /// Creates a <see cref="RegisterStep"/> for receive behavior.
        /// </summary>
        protected override Func<IBuilder, ReceiveBehavior> GetReceiveBehaviorFactory(ReceiveOptions receiveOptions)
        {
            options = receiveOptions;

            if (!receiveOptions.Transactions.IsTransactional)
            {
                return b => new MsmqReceiveWithNoTransactionBehavior();
            }

            if (receiveOptions.Transactions.SuppressDistributedTransactions)
            {
                return b => new MsmqReceiveWithNativeTransactionBehavior();
            }
            else
            {
                return b =>
                {
                    var transactionOptions = new TransactionOptions
                    {
                        IsolationLevel = receiveOptions.Transactions.IsolationLevel,
                        Timeout = receiveOptions.Transactions.TransactionTimeout
                    };

                    return new MsmqReceiveWithTransactionScopeBehavior(transactionOptions);
                };
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigureTransport"/>.
        /// </summary>
        protected override void Configure(FeatureConfigurationContext context, string connectionString)
        {
            new CheckMachineNameForComplianceWithDtcLimitation().Check();

            var endpointIsTransactional = context.Settings.Get<bool>("Transactions.Enabled");
           
            Func<IReadOnlyDictionary<string, string>, string> getMessageLabel;
            context.Settings.TryGet("Msmq.GetMessageLabel", out getMessageLabel);
            if (!context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                //todo: move this to the external distributor
                //var workerRunsOnThisEndpoint = settings.GetOrDefault<bool>("Worker.Enabled");

                //if (workerRunsOnThisEndpoint
                //    && (returnAddressForFailures.Queue.ToLower().EndsWith(".worker") || address == config.LocalAddress))
                //    //this is a hack until we can refactor the SLR to be a feature. "Worker" is there to catch the local worker in the distributor
                //{
                //    returnAddressForFailures = settings.Get<Address>("MasterNode.Address");

                //    Logger.InfoFormat("Worker started, failures will be redirected to {0}", returnAddressForFailures);
                //}


                context.Container.ConfigureComponent(b => new MsmqDequeueStrategy(b.Build<CriticalError>(), endpointIsTransactional, MsmqAddress.Parse(options.ErrorQueue)),
                    DependencyLifecycle.InstancePerCall);
            }

            var settings = new MsmqSettings();
            if (connectionString != null)
            {
                settings = new MsmqConnectionStringBuilder(connectionString).RetrieveSettings();
            }

            var messageLabelGenerator = context.Settings.GetMessageLabelGenerator();
            context.Container.ConfigureComponent(b=>new MsmqMessageSender(settings, messageLabelGenerator), DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent<MsmqQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Settings, settings);
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

        ReceiveOptions options;
    }
}