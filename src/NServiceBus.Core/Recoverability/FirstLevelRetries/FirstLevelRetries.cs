namespace NServiceBus.Features
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Config;
    using NServiceBus.Recoverability.FirstLevelRetries;
    using NServiceBus.Settings;

    /// <summary>
    /// Used to configure Second Level Retries.
    /// </summary>
    public class FirstLevelRetries : Feature
    {
        internal FirstLevelRetries()
        {
            EnableByDefault();
            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't use FLR since it only applies to messages being received");

            Prerequisite(context => context.Settings.Get<bool>("Transactions.Enabled"), "Send only endpoints can't use FLR since it requires the transport to be able to rollback");

            Prerequisite(context => GetMaxRetries(context.Settings) > 0, "FLR was disabled in config since it's set to 0");
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var settings = context.Settings;
            var transportConfig = settings.GetConfigSection<TransportConfig>();
            var maxRetries = transportConfig?.MaxRetries ?? 5;
            var retryPolicy = new FirstLevelRetryPolicy(maxRetries);

            var flrStatusStorage = new FlrStatusStorage();
            context.RegisterStartupTask(new FlrStatusStorageCleaner(flrStatusStorage));

            context.Pipeline.Register<FirstLevelRetriesBehavior.Registration>();

            context.Container.ConfigureComponent(b =>
            {
                var firstLevelRetryActions = settings.GetFirstLevelRetryActions();
                return new FirstLevelRetriesBehavior(flrStatusStorage, retryPolicy, firstLevelRetryActions);
            }, DependencyLifecycle.InstancePerCall);
        }
       

        int GetMaxRetries(ReadOnlySettings settings)
        {
            var retriesConfig = settings.GetConfigSection<TransportConfig>();

            if (retriesConfig == null)
                return 5;

            return retriesConfig.MaxRetries;

        }

        class FlrStatusStorageCleaner : FeatureStartupTask
        {
            public FlrStatusStorageCleaner(FlrStatusStorage statusStorage)
            {
                this.statusStorage = statusStorage;
            }

            protected override Task OnStart(IBusContext context)
            {
                timer = new Timer(ClearFlrStatusStorage, null, ClearingInterval, ClearingInterval);
                return TaskEx.Completed;
            }

            protected override Task OnStop(IBusContext context)
            {
                timer?.Dispose();
                return TaskEx.Completed;
            }

            void ClearFlrStatusStorage(object state)
            {
                statusStorage.ClearAllFailures();
            }

            static readonly TimeSpan ClearingInterval = TimeSpan.FromMinutes(5);
            FlrStatusStorage statusStorage;
            Timer timer;
        }

    }
}