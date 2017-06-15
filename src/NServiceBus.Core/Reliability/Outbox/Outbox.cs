namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using ConsistencyGuarantees;
    using Logging;
    using Persistence;

    /// <summary>
    /// Configure the Outbox.
    /// </summary>
    public class Outbox : Feature
    {
        internal Outbox()
        {
            Defaults(s => s.SetDefault(InMemoryOutboxPersistence.TimeToKeepDeduplicationEntries, TimeSpan.FromDays(5)));

            Prerequisite(c => c.Settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None, "Outbox isn't needed since the receive transactions has been turned off");
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

        static ILog log = LogManager.GetLogger<Outbox>();
    }

    class DtcRunningWarning : FeatureStartupTask
    {
        protected override Task OnStart(IMessageSession session)
        {
#if NET452
            try
            {
                var sc = new System.ServiceProcess.ServiceController
                {
                    ServiceName = "MSDTC",
                    MachineName = "."
                };

                if (sc.Status == System.ServiceProcess.ServiceControllerStatus.Running)
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
#endif

            return TaskEx.CompletedTask;
        }

        protected override Task OnStop(IMessageSession session)
        {
            return TaskEx.CompletedTask;
        }

        static ILog log = LogManager.GetLogger<DtcRunningWarning>();
    }
}