namespace NServiceBus.Features
{
    using System.Runtime.InteropServices;
    using System.Text;
    using Config;
    using Logging;
    using Timeout;
    using Transports;
    using Transports.Msmq;
    using Transports.Msmq.Config;

    /// <summary>
    /// Used to configure the MSMQ transport.
    /// </summary>
    public class MsmqTransport : ConfigureTransport<Msmq>
    {
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
                .ConfigureProperty(t => t.Settings, settings);

            context.Container.ConfigureComponent<MsmqQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Settings, settings);

            context.Container.ConfigureComponent<TimeoutManagerDeferrer>(DependencyLifecycle.InstancePerCall)
              .ConfigureProperty(p => p.TimeoutManagerAddress, GetTimeoutManagerAddress(context));
        }

        protected override void InternalConfigure(Configure config)
        {
            config.Features(f =>
            {
                f.Enable<MsmqTransport>();
                f.Enable<MessageDrivenSubscriptions>();
            });

            config.Settings.EnableFeatureByDefault<StorageDrivenPublishing>();
            config.Settings.EnableFeatureByDefault<TimeoutManager>();

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

        static ILog Logger = LogManager.GetLogger<MsmqTransport>();

        const string Message =
            @"
MsmqMessageQueueConfig section has been deprecated in favor of using a connectionString instead.
Here is an example of what is required:
  <connectionStrings>
    <add name=""NServiceBus/Transport"" connectionString=""cacheSendConnection=true;journal=false;deadLetter=true"" />
  </connectionStrings>";



    }

    enum COMPUTER_NAME_FORMAT
    {
        ComputerNameNetBIOS,
        ComputerNameDnsHostname,
        ComputerNameDnsDomain,
        ComputerNameDnsFullyQualified,
        ComputerNamePhysicalNetBIOS,
        ComputerNamePhysicalDnsHostname,
        ComputerNamePhysicalDnsDomain,
        ComputerNamePhysicalDnsFullyQualified
    }

    public class CheckMachineNameForComplianceWithDtcLimitation
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetComputerNameEx(COMPUTER_NAME_FORMAT nameType, [Out] StringBuilder lpBuffer, ref uint lpnSize);

        static ILog Logger = LogManager.GetLogger<CheckMachineNameForComplianceWithDtcLimitation>();

        /// <summary>
        /// Method invoked to run custom code.
        /// </summary>
        public void Check()
        {

            uint capacity = 24;
            var buffer = new StringBuilder((int)capacity);
            if (!GetComputerNameEx(COMPUTER_NAME_FORMAT.ComputerNameNetBIOS, buffer, ref capacity))
                return;
            var netbiosName = buffer.ToString();
            if (netbiosName.Length <= 15) return;

            Logger.Warn(string.Format(
                "NetBIOS name [{0}] is longer than 15 characters. Shorten it for DTC to work. See: http://particular.net/articles/dtcping-warning-the-cid-values-for-both-test-machines-are-the-same", netbiosName));
        }
    }
}