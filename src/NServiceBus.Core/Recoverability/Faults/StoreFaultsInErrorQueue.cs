namespace NServiceBus.Features
{
    using NServiceBus.Transports;

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
            var registerStep = new MoveFaultsToErrorQueueBehavior.Registration(context.Settings, context.Settings.LocalAddress());
            context.Pipeline.Register(registerStep);
        }


    }
}