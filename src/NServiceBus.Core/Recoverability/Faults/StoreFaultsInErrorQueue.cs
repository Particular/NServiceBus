namespace NServiceBus.Features
{
    using Transports;

    class StoreFaultsInErrorQueue : Feature
    {
        internal StoreFaultsInErrorQueue()
        {
            EnableByDefault();
            Prerequisite(context =>
            {
                var b = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
                return b;
            }, "Send only endpoints can't be used to forward received messages to the error queue as the endpoint requires receive capabilities");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(context.Settings);
            context.Settings.Get<QueueBindings>().BindSending(errorQueue);
            context.Pipeline.Register(new MoveFaultsToErrorQueueBehavior.Registration(errorQueue, context.Settings.LocalAddress()));
            context.Pipeline.Register("FaultToDispatchConnector", new FaultToDispatchConnector(), "Connector to dispatch faulted messages");
        }
    }
}