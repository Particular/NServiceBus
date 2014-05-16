namespace NServiceBus.Features
{
    using Config;
    using Logging;
    using Settings;
    using Transports;
    using Transports.Msmq;
    using Transports.Msmq.Config;

    public class MsmqTransport:ConfigureTransport<Msmq>
    {
        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<CorrelationIdMutatorForBackwardsCompatibilityWithV3>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<MsmqUnitOfWork>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<MsmqDequeueStrategy>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested);
          
            var cfg = NServiceBus.Configure.GetConfigSection<MsmqMessageQueueConfig>();

            var settings = new MsmqSettings();
            if (cfg != null)
            {
                settings.UseJournalQueue = cfg.UseJournalQueue;
                settings.UseDeadLetterQueue = cfg.UseDeadLetterQueue;

                Logger.Warn(Message);
            }
            else
            {
                var connectionString = config.Settings.Get<string>("NServiceBus.Transport.ConnectionString");
         
                if (connectionString != null)
                {
                    settings = new MsmqConnectionStringBuilder(connectionString).RetrieveSettings();
                }
            }

            config.Configurer.ConfigureComponent<MsmqMessageSender>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Settings, settings);

            config.Configurer.ConfigureComponent<MsmqQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Settings, settings);
        }

        protected override void InternalConfigure(Configure config)
        {
            Enable<MsmqTransport>();
            Enable<MessageDrivenSubscriptions>();

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

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ConfigureMsmqMessageQueue));

        private const string Message =
            @"
MsmqMessageQueueConfig section has been deprecated in favor of using a connectionString instead.
Here is an example of what is required:
  <connectionStrings>
    <add name=""NServiceBus/Transport"" connectionString=""cacheSendConnection=true;journal=false;deadLetter=true"" />
  </connectionStrings>";
    }
}