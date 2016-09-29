namespace NServiceBus.Features
{
    using System;
    using System.Configuration;
    using System.ServiceProcess;
    using System.Threading.Tasks;
    using ConsistencyGuarantees;
    using Logging;
    using Persistence;
    using Transport;

    /// <summary>
    /// Configure the Outbox.
    /// </summary>
    public class Outbox : Feature
    {
        internal Outbox()
        {
            Defaults(s => s.SetDefault(InMemoryOutboxPersistence.TimeToKeepDeduplicationEntries, TimeSpan.FromDays(5)));

            Prerequisite(c => c.Settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None, "Outbox isn't needed since the receive transactions has been turned off");

            Prerequisite(c =>
            {
                if (!c.Settings.Get<TransportInfrastructure>().RequireOutboxConsent)
                {
                    return true;
                }

                return RequireOutboxConsent(c);
            }, "This transport requires outbox consent");
        }

        static bool RequireOutboxConsent(FeatureConfigurationContext context)
        {
            if (context.Settings.GetOrDefault<bool>("DisableOutboxTransportCheck"))
            {
                return true;
            }
            var configValue = ConfigurationManager.AppSettings.Get("NServiceBus/Outbox");

            if (configValue == null)
            {
                throw new Exception(@"To use the Outbox feature with MSMQ or SQLServer transports it must be enabled in the config file.
To do that add the following:
<appSettings>
    <add key=""NServiceBus/Outbox"" value=""true""/>
</appSettings>

The reason this is required is to ensure that all the guidelines regarding this feature have been understood and know the limitations when running under MSMQ or SQLServer transports.");
            }

            bool result;

            if (!bool.TryParse(configValue, out result))
            {
                throw new Exception("Invalid value in \"NServiceBus/Outbox\" AppSetting. Ensure it is either \"true\" or \"false\".");
            }

            return result;
        }


        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (!PersistenceStartup.HasSupportFor<StorageType.Outbox>(context.Settings))
            {
                throw new Exception("The selected persistence doesn't have support for outbox storage. Select another persistence or disable the outbox feature using endpointConfiguration.DisableFeature<Outbox>()");
            }

            //note: in the future we should change the persister api to give us a "outbox factory" so that we can register it in DI here instead of relying on the persister to do it

            context.RegisterStartupTask(new DtcRunningWarning());
            context.Pipeline.Register("ForceBatchDispatchToBeIsolated", new ForceBatchDispatchToBeIsolatedBehavior(), "Makes sure that we dispatch straight to the transport so that we can safely set the outbox record to dispatched one the dispatch pipeline returns.");
        }
    }

    class DtcRunningWarning : FeatureStartupTask
    {
        protected override Task OnStart(IMessageSession session)
        {
            try
            {
                var sc = new ServiceController
                {
                    ServiceName = "MSDTC",
                    MachineName = "."
                };

                if (sc.Status == ServiceControllerStatus.Running)
                {
                    log.Warn(@"The MSDTC service is running on this machine.
Because Outbox is enabled disabling MSDTC is recommended. This ensures that the Outbox behavior is working as expected and no other resources are enlisting in distributed transactions.");
                }
            }
                // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
                // Ignore if we can't check it.
            }

            return TaskEx.CompletedTask;
        }

        protected override Task OnStop(IMessageSession session)
        {
            return TaskEx.CompletedTask;
        }

        static ILog log = LogManager.GetLogger<DtcRunningWarning>();
    }
}