namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Saga;
    using NServiceBus.Unicast.Behaviors;

    class PrepareConsumeTimoutContextBehavior : HandlingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var messageHandler = context.MessageHandler;
            if (messageHandler.HandlerKind == HandlerKind.Timeout)
            {
                context.Set("InvocationContext", new ConsumeTimeoutContext());
            }

            next();
        }
    }
}