namespace NServiceBus.Features
{
    using System;
    using System.Transactions;
    using Config;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using Transports;
    using Transports.Msmq;
    using Transports.Msmq.Config;

    /// <summary>
    /// Used to configure the MSMQ transport.
    /// </summary>
    public class MsmqTransportConfigurator : ConfigureTransport
    {
        internal MsmqTransportConfigurator()
        {
            DependsOn<UnicastBus>();
        }

        /// <summary>
        /// Creates a <see cref="RegisterStep"/> for receive behavior.
        /// </summary>
        /// <returns></returns>
        protected override Func<IBuilder, ReceiveBehavior> GetReceiveBehaviorFactory(ReceiveOptions receiveOptions)
        {
            options = receiveOptions;


            if (!receiveOptions.Transactions.IsTransactional)
            {
                return b=> new MsmqReceiveWithNoTransactionBehavior();
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
            new CheckMachineNameForComplianceWithDtcLimitation()
            .Check();

            context.Container.ConfigureComponent<MsmqUnitOfWork>(DependencyLifecycle.SingleInstance);

            var endpointIsTransactional = context.Settings.Get<bool>("Transactions.Enabled");
            var doNotUseDTCTransactions = context.Settings.Get<bool>("Transactions.SuppressDistributedTransactions");


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

            context.Container.ConfigureComponent<MsmqMessageSender>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Settings, settings)
                .ConfigureProperty(t => t.SuppressDistributedTransactions, doNotUseDTCTransactions);

            context.Container.ConfigureComponent<MsmqQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Settings, settings);
        }

        /// <summary>
        /// <see cref="ConfigureTransport.ExampleConnectionStringForErrorMessage"/>
        /// </summary>
        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "cacheSendConnection=true;journal=false;deadLetter=true"; }
        }

        /// <summary>
        /// <see cref="ConfigureTransport.RequiresConnectionString"/>
        /// </summary>
        protected override bool RequiresConnectionString
        {
            get { return false; }
        }

        ReceiveOptions options;
    }

}