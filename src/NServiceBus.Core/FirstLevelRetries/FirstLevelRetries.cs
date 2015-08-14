namespace NServiceBus.Features
{
    using System;
    using System.Timers;
    using Config;
    using NServiceBus.FirstLevelRetries;
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

            RegisterStartupTask<FlrStatusStorageCleaner>();
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportConfig = context.Settings.GetConfigSection<TransportConfig>();
            var maxRetries = transportConfig != null ? transportConfig.MaxRetries : 5;
            var retryPolicy = new FirstLevelRetryPolicy(maxRetries);
            context.Container.RegisterSingleton(retryPolicy);

            var flrStatusStorage = new FlrStatusStorage();
            context.Container.RegisterSingleton(flrStatusStorage);

            context.Pipeline.Register<FirstLevelRetriesBehavior.Registration>();
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
            FlrStatusStorage statusStorage;
            Timer timer;

            public FlrStatusStorageCleaner(FlrStatusStorage statusStorage)
            {
                this.statusStorage = statusStorage;

                timer = new Timer
                {
                    Interval = TimeSpan.FromSeconds(10).TotalMilliseconds,
                    AutoReset = true,
                    SynchronizingObject = null
                };
                timer.Elapsed += ClearFlrStatusStorage;
            }

            protected override void OnStart()
            {
                timer.Start();
            }

            protected override void OnStop()
            {
                timer.Stop();
                //TODO: can't implement IDisposable in nested class bc. Fody.Janitor
            }

            void ClearFlrStatusStorage(object sender, ElapsedEventArgs e)
            {
                statusStorage.Clear();
            }
        }

    }
}