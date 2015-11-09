namespace NServiceBus.Features
{
    using System;
    using System.Threading;
    using NServiceBus.Faults;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline;
    using NServiceBus.Recoverability.Faults;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class StoreFaultsInErrorQueue : Feature
    {
        internal StoreFaultsInErrorQueue()
        {
            EnableByDefault();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't be used to forward received messages to the error queue as the endpoint requires receive capabilities");

            RegisterStartupTask<FaultsStatusStorageCleaner>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {

            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(context.Settings);

            var faultsStorage = new FaultsStatusStorage();
            context.Container.RegisterSingleton(typeof(FaultsStatusStorage), faultsStorage);

            context.Container.ConfigureComponent(b =>
            {
                var pipelinesCollection = context.Settings.Get<PipelineConfiguration>();

                var dispatchPipeline = new PipelineBase<RoutingContext>(b, context.Settings, pipelinesCollection.MainPipeline);

                return new MoveFaultsToErrorQueueBehavior(
                    b.Build<CriticalError>(),
                    dispatchPipeline,
                    b.Build<HostInformation>(),
                    b.Build<BusNotifications>(),
                    errorQueue, 
                    faultsStorage);
            }, DependencyLifecycle.InstancePerCall);

            context.Settings.Get<QueueBindings>().BindSending(errorQueue);

            context.Pipeline.Register<MoveFaultsToErrorQueueBehavior.Registration>();
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