namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Saga;
    using NServiceBus.Unicast.Behaviors;

    class PrepareTimoutContextBehavior : HandlingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var messageHandler = context.MessageHandler;
            if (messageHandler.HandlerKind == HandlerKind.Timeout)
            {
                context.Set("InvocationContext", new TimeoutContext());
            }

            next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("PrepareTimeoutContext", typeof(PrepareTimoutContextBehavior), "If the current handler handles timeouts a timeout context is added as invocation context.")
            {
                InsertBeforeIfExists(WellKnownStep.InvokeHandlers);
            }
        }
    }
}