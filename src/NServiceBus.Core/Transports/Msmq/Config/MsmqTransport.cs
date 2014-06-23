namespace NServiceBus.Features
{
    using Config;
    using Logging;
    using Transports;
    using Transports.Msmq;
    using Transports.Msmq.Config;

    /// <summary>
    /// Used to configure the MSMQ transport.
    /// </summary>
    public class MsmqTransport : ConfigureTransport<Msmq>
    {
        internal MsmqTransport()
        {
            
        }

        /// <summary>
        /// <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            new CheckMachineNameForComplianceWithDtcLimitation()
            .Check();
            context.Container.ConfigureComponent<CorrelationIdMutatorForBackwardsCompatibilityWithV3>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<MsmqUnitOfWork>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<MsmqDequeueStrategy>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested);

            var cfg = context.Settings.GetConfigSection<MsmqMessageQueueConfig>();

            var settings = new MsmqSettings();
            if (cfg != null)
            {
                settings.UseJournalQueue = cfg.UseJournalQueue;
                settings.UseDeadLetterQueue = cfg.UseDeadLetterQueue;

                Logger.Warn(Message);
            }
            else
            {
                var connectionString = context.Settings.Get<string>("NServiceBus.Transport.ConnectionString");

                if (connectionString != null)
                {
                    settings = new MsmqConnectionStringBuilder(connectionString).RetrieveSettings();
                }
            }

            context.Container.ConfigureComponent<MsmqMessageSender>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Settings, settings)
                .ConfigureProperty(t => t.SuppressDistributedTransactions, context.Settings.Get<bool>("Transactions.SuppressDistributedTransactions"));

            context.Container.ConfigureComponent<MsmqQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Settings, settings);
        }

        /// <summary>
        /// <see cref="ConfigureTransport{T}.InternalConfigure"/>
        /// </summary>
        protected override void InternalConfigure(Configure config)
        {
            config.EnableFeature<MsmqTransport>();
            config.EnableFeature<MessageDrivenSubscriptions>();
            config.EnableFeature<TimeoutManagerBasedDeferral>();

            config.Settings.EnableFeatureByDefault<StorageDrivenPublishing>();
            config.Settings.EnableFeatureByDefault<TimeoutManager>();
        }

        /// <summary>
        /// <see cref="ConfigureTransport{T}.ExampleConnectionStringForErrorMessage"/>
        /// </summary>
        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "cacheSendConnection=true;journal=false;deadLetter=true"; }
        }

        /// <summary>
        /// <see cref="ConfigureTransport{T}.RequiresConnectionString"/>
        /// </summary>
        protected override bool RequiresConnectionString
        {
            get { return false; }
        }

        static ILog Logger = LogManager.GetLogger<MsmqTransport>();

        const string Message =
            @"
MsmqMessageQueueConfig section has been deprecated in favor of using a connectionString instead.
Here is an example of what is required:
  <connectionStrings>
    <add name=""NServiceBus/Transport"" connectionString=""cacheSendConnection=true;journal=false;deadLetter=true"" />
  </connectionStrings>";



    }

}