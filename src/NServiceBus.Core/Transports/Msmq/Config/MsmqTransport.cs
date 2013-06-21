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
        public override void Initialize()
        {
            NServiceBus.Configure.Component<CorrelationIdMutatorForBackwardsCompatibilityWithV3>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<MsmqUnitOfWork>(DependencyLifecycle.SingleInstance);
            NServiceBus.Configure.Component<MsmqDequeueStrategy>(DependencyLifecycle.InstancePerCall)
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
                var connectionString = SettingsHolder.Get<string>("NServiceBus.Transport.ConnectionString");
         
                if (connectionString != null)
                {
                    settings = new MsmqConnectionStringBuilder(connectionString).RetrieveSettings();
                }
            }

            NServiceBus.Configure.Component<MsmqMessageSender>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Settings, settings);

            NServiceBus.Configure.Component<MsmqQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Settings, settings);
        }

        protected override void InternalConfigure(Configure config)
        {
            Enable<MsmqTransport>();
            Enable<MessageDrivenSubscriptions>();

            //for backwards compatibility
            SettingsHolder.SetDefault("SerializationSettings.WrapSingleMessages", true);
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