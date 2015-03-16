namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Behaviors;

    class PrepareSubscribeContextBehavior : HandlingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var messageHandler = context.MessageHandler;
            if (messageHandler.HandlerKind == HandlerKind.Event)
            {
                context.Set("InvocationContext", new SubscribeContext());
            }

            next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("PrepareSubscribeContext", typeof(PrepareSubscribeContextBehavior), "If the current handler handles events a subscribe context is added as invocation context.")
            {
                InsertBeforeIfExists(WellKnownStep.InvokeHandlers);
            }
        }
    }
}