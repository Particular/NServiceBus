namespace NServiceBus.Features
{
    using System;
    using System.Configuration;
    using System.ServiceProcess;
    using Logging;
    using NServiceBus.Outbox;
    using Persistence;
    using Pipeline;
    using Transports;
    using Unicast.Transport;

    /// <summary>
    /// Configure the Outbox.
    /// </summary>
    public class Outbox : Feature
    {
        internal Outbox()
        {
            Defaults(s => s.SetDefault(TimeToKeepDeduplicationEntries, TimeSpan.FromDays(5)));

            Prerequisite(c => c.Settings.Get<bool>("Transactions.Enabled"),"Outbox isn't needed since the receive transactions has been turned off");

            Prerequisite(c =>
            {
                if (!c.Settings.Get<TransportDefinition>().RequireOutboxConsent)
                {
                    return true;
                }

                return RequireOutboxConsent(c);
            },"This transport requires outbox consent");

            RegisterStartupTask<DtcRunningWarning>();
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
                throw new Exception(@"To use the Outbox feature with MSMQ or SQLServer transports you need to enable it in your config file.
To do that add the following:
<appSettings>
    <add key=""NServiceBus/Outbox"" value=""true""/>
</appSettings>

The reason you need to do this is because we need to ensure that you have read all the documentation regarding this feature and know the limitations when running it under MSMQ or SQLServer transports.");
            }

            bool result;

            if (!Boolean.TryParse(configValue, out result))
            {
                throw new Exception("Invalid value in \"NServiceBus/Outbox\" AppSetting. Please ensure it is either \"true\" or \"false\".");
            }

            return result;
        }

        internal const string TimeToKeepDeduplicationEntries = "Outbox.TimeToKeepDeduplicationEntries";

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (!PersistenceStartup.HasSupportFor<StorageType.Outbox>(context.Settings))
            {
                throw new Exception("Selected persister doesn't have support for outbox storage. Please select another storage or disable the outbox feature using config.Features(f=>f.Disable<Outbox>())");    
            }

            context.Pipeline.Register<OutboxDeduplicationBehavior.OutboxDeduplicationRegistration>();
            context.Pipeline.Register<OutboxRecordBehavior.OutboxRecorderRegistration>();
            context.Pipeline.Replace(WellKnownStep.DispatchMessageToTransport, typeof(OutboxSendBehavior), "Sending behavior with a delay sending until all business transactions are committed to the outbox storage");

            context.Container.ConfigureComponent(
                b => new OutboxDeduplicationBehavior(b.Build<IOutboxStorage>(),
                    b.Build<DispatchMessageToTransportBehavior>(), 
                    b.Build<DefaultMessageAuditer>(),
                    new TransactionSettings(context.Settings)), 
                DependencyLifecycle.InstancePerCall);

            //make the audit use the outbox as well
            context.Container.ConfigureComponent<OutboxAwareAuditer>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureProperty<AuditerWrapper>(t => t.AuditerImplType, typeof(OutboxAwareAuditer));
        }

    }

    class DtcRunningWarning :FeatureStartupTask
    {
        protected override void OnStart()
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
                    log.Warn(@"We have detected that MSDTC service is running on your machine.
Because you have configured this endpoint to run with Outbox enabled we recommend turning MSDTC off to ensure that the Outbox behavior is working as expected and no other resources are enlisting in distributed transactions.");
                }
            }
// ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
                // Ignore if we can't check it.
            }
            
        }

        static ILog log = LogManager.GetLogger<DtcRunningWarning>();
    }
}