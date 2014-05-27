namespace NServiceBus.Features
{
    using Config;
    using Logging;
    using Timeout;
    using Transports;
    using Transports.Msmq;
    using Transports.Msmq.Config;

    public class MsmqTransport : ConfigureTransport<Msmq>
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
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
                .ConfigureProperty(t => t.Settings, settings);

            context.Container.ConfigureComponent<MsmqQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Settings, settings);

            context.Container.ConfigureComponent<TimeoutManagerDeferrer>(DependencyLifecycle.InstancePerCall)
              .ConfigureProperty(p => p.TimeoutManagerAddress, GetTimeoutManagerAddress(context));
        }

        protected override void InternalConfigure(Configure config)
        {
            config.Features.Enable<MsmqTransport>();
            config.Features.Enable<MessageDrivenSubscriptions>();
            config.Features.EnableByDefault<StorageDrivenPublishing>();
            config.Features.EnableByDefault<TimeoutManager>();

            //for backwards compatibility
            config.Settings.SetDefault("SerializationSettings.WrapSingleMessages", true);
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "cacheSendConnection=true;journal=false;deadLetter=true"; }
        }

        protected override bool RequiresConnectionString
        {
            get { return false; }
        }

        static Address GetTimeoutManagerAddress(FeatureConfigurationContext context)
        {
            var unicastConfig = context.Settings.GetConfigSection<UnicastBusConfig>();

            if (unicastConfig != null && !string.IsNullOrWhiteSpace(unicastConfig.TimeoutManagerAddress))
            {
                return Address.Parse(unicastConfig.TimeoutManagerAddress);
            }

            return context.Settings.Get<Address>("MasterNode.Address").SubScope("Timeouts");
        }

        static ILog Logger = LogManager.GetLogger < MsmqTransport>();

        const string Message =
            @"
MsmqMessageQueueConfig section has been deprecated in favor of using a connectionString instead.
Here is an example of what is required:
  <connectionStrings>
    <add name=""NServiceBus/Transport"" connectionString=""cacheSendConnection=true;journal=false;deadLetter=true"" />
  </connectionStrings>";
    }
}