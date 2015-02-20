namespace NServiceBus.Features
{
    using NServiceBus.Faults;
    using NServiceBus.Faults.Forwarder.Config;
    using NServiceBus.Hosting;
    using NServiceBus.Transports;

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

            context.Container.ConfigureComponent(b => new MoveFaultsToErrorQueueBehavior(
                b.Build<CriticalError>(),
                b.Build<ISendMessages>(),
                b.Build<HostInformation>(),
                b.Build<BusNotifications>(), 
                errorQueue.ToString()), DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent<FaultsQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.Enabled, true)
                .ConfigureProperty(t => t.ErrorQueue, errorQueue);

            context.Pipeline.Register<MoveFaultsToErrorQueueBehavior.Registration>();
        }


    }
}