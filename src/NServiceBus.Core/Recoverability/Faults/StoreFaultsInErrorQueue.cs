namespace NServiceBus.Features
{
    using System;
    using System.Threading;
    using NServiceBus.Config;
    using NServiceBus.Faults;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline;
    using NServiceBus.Recoverability.Faults;
    using NServiceBus.Recoverability.FirstLevelRetries;
    using NServiceBus.Recoverability.SecondLevelRetries;
    using NServiceBus.Settings;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class StoreFaultsInErrorQueue : Feature
    {
        internal StoreFaultsInErrorQueue()
        {
            EnableByDefault();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't be used to forward received messages to the error queue as the endpoint requires receive capabilities");

            RegisterStartupTask<FaultsStatusStorageCleaner>();

            //SLR
            //This should check if we can perform SLR and enable/disable this sub-moduel
            /*
            DependsOn<DelayedDeliveryFeature>();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't use SLR since it requires receive capabilities");

            Prerequisite(IsEnabledInConfig, "SLR was disabled in config");
            */
            RegisterStartupTask<SlrStatusStorageCleaner>();


            //FLR
            //This should check if we can perform FLR and enable/disable this sub-moduel
            /*
            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't use FLR since it only applies to messages being received");

            Prerequisite(context => context.Settings.Get<bool>("Transactions.Enabled"), "Send only endpoints can't use FLR since it requires the transport to be able to rollback");

            Prerequisite(context => GetMaxRetries(context.Settings) > 0, "FLR was disabled in config since it's set to 0");
            */
            RegisterStartupTask<FlrStatusStorageCleaner>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            //Faults support
            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(context.Settings);

            var faultsStorage = new FaultsStatusStorage();
            context.Container.RegisterSingleton(typeof(FaultsStatusStorage), faultsStorage);

            context.Settings.Get<QueueBindings>().BindSending(errorQueue);

            context.Pipeline.Register<MoveFaultsToErrorQueueBehavior.Registration>();

            //SLR
            var retryPolicy = GetRetryPolicy(context.Settings);
            var slrStorage = new SlrStatusStorage();

            context.Container.RegisterSingleton(typeof(SecondLevelRetryPolicy), retryPolicy);
            context.Container.RegisterSingleton(typeof(SlrStatusStorage), slrStorage);

            //FLR
            var transportConfig = context.Settings.GetConfigSection<TransportConfig>();
            var maxRetries = transportConfig?.MaxRetries ?? 5;
            var flrRetryPolicy = new FirstLevelRetryPolicy(maxRetries);
            context.Container.RegisterSingleton(flrRetryPolicy);

            var flrStatusStorage = new FlrStatusStorage();
            context.Container.RegisterSingleton(typeof(FlrStatusStorage), flrStatusStorage);

            context.Container.ConfigureComponent(b =>
            {
                var pipelinesCollection = context.Settings.Get<PipelineConfiguration>();

                var dispatchPipeline = new PipelineBase<RoutingContext>(b, context.Settings, pipelinesCollection.MainPipeline);

                var flrHandler = new FirstLevelRetriesHandler(flrStatusStorage, flrRetryPolicy, b.Build<BusNotifications>());

                var slrHandler = new SecondLevelRetriesHandler(dispatchPipeline, retryPolicy, b.Build<BusNotifications>(), context.Settings.LocalAddress(), slrStorage, IsEnabledInConfig(context));

                return new MoveFaultsToErrorQueueBehavior(
                    b.Build<CriticalError>(),
                    dispatchPipeline,
                    b.Build<HostInformation>(),
                    b.Build<BusNotifications>(),
                    errorQueue,
                    faultsStorage,
                    flrHandler,
                    slrHandler);
            }, DependencyLifecycle.InstancePerCall);
        }

        int GetMaxRetries(ReadOnlySettings settings)
        {
            var retriesConfig = settings.GetConfigSection<TransportConfig>();

            if (retriesConfig == null)
                return 5;

            return retriesConfig.MaxRetries;

        }

        bool IsEnabledInConfig(FeatureConfigurationContext context)
        {
            var retriesConfig = context.Settings.GetConfigSection<SecondLevelRetriesConfig>();

            if (retriesConfig == null)
                return true;

            if (retriesConfig.NumberOfRetries == 0)
                return false;

            return retriesConfig.Enabled;
        }

        static SecondLevelRetryPolicy GetRetryPolicy(ReadOnlySettings settings)
        {
            var customRetryPolicy = settings.GetOrDefault<Func<IncomingMessage, TimeSpan>>("SecondLevelRetries.RetryPolicy");

            if (customRetryPolicy != null)
            {
                return new CustomSecondLevelRetryPolicy(customRetryPolicy);
            }

            var retriesConfig = settings.GetConfigSection<SecondLevelRetriesConfig>();
            if (retriesConfig != null)
            {
                return new DefaultSecondLevelRetryPolicy(retriesConfig.NumberOfRetries, retriesConfig.TimeIncrease);
            }

            return new DefaultSecondLevelRetryPolicy(DefaultSecondLevelRetryPolicy.DefaultNumberOfRetries, DefaultSecondLevelRetryPolicy.DefaultTimeIncrease);
        }

        class SlrStatusStorageCleaner : FeatureStartupTask
        {
            public SlrStatusStorageCleaner(SlrStatusStorage statusStorage)
            {
                this.statusStorage = statusStorage;
            }

            protected override void OnStart()
            {
                timer = new Timer(ClearSlrStatusStorage, null, ClearingInterval, ClearingInterval);
            }

            protected override void OnStop()
            {
                timer?.Dispose();
            }

            void ClearSlrStatusStorage(object state)
            {
                statusStorage.ClearAllExceptions();
            }

            static readonly TimeSpan ClearingInterval = TimeSpan.FromMinutes(5);
            SlrStatusStorage statusStorage;
            Timer timer;
        }

        class FlrStatusStorageCleaner : FeatureStartupTask
        {
            public FlrStatusStorageCleaner(FlrStatusStorage statusStorage)
            {
                this.statusStorage = statusStorage;
            }

            protected override void OnStart()
            {
                timer = new Timer(ClearFlrStatusStorage, null, ClearingInterval, ClearingInterval);
            }

            protected override void OnStop()
            {
                timer?.Dispose();
            }

            void ClearFlrStatusStorage(object state)
            {
                statusStorage.ClearAllFailures();
            }

            static readonly TimeSpan ClearingInterval = TimeSpan.FromMinutes(5);
            FlrStatusStorage statusStorage;
            Timer timer;
        }

        class FaultsStatusStorageCleaner : FeatureStartupTask
        {
            public FaultsStatusStorageCleaner(FaultsStatusStorage statusStorage)
            {
                this.statusStorage = statusStorage;
            }

            protected override void OnStart()
            {
                timer = new Timer(ClearFaultsStatusStorage, null, ClearingInterval, ClearingInterval);
            }

            protected override void OnStop()
            {
                timer?.Dispose();
            }

            void ClearFaultsStatusStorage(object state)
            {
                statusStorage.ClearAllExceptions();
            }

            static readonly TimeSpan ClearingInterval = TimeSpan.FromMinutes(5);
            FaultsStatusStorage statusStorage;
            Timer timer;
        }
    }
}