namespace NServiceBus.Features
{
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline;
    using NServiceBus.Recoverability.Faults;
    using NServiceBus.TransportDispatch;

    class StoreFaultsInErrorQueue : Feature
    {
        internal StoreFaultsInErrorQueue()
        {
            EnableByDefault();
            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't be used to forward received messages to the error queue as the endpoint requires receive capabilities");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {

            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(context.Settings);

            context.Container.ConfigureComponent(b =>
            {
                var pipelinesCollection = context.Settings.Get<PipelineConfiguration>();

                var dispatchPipeline = new PipelineBase<DispatchContext>(b, context.Settings, pipelinesCollection.MainPipeline);

                return new MoveFaultsToErrorQueueBehavior(
                    b.Build<CriticalError>(),
                    dispatchPipeline,
                    b.Build<HostInformation>(),
                    b.Build<BusNotifications>(),
                    errorQueue.ToString());
            }, DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent<FaultsQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.Enabled, true)
                .ConfigureProperty(t => t.ErrorQueue, errorQueue);

            context.Pipeline.Register<MoveFaultsToErrorQueueBehavior.Registration>();
        }


    }
}