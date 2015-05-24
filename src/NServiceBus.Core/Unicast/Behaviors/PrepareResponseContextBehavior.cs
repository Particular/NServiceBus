namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Behaviors;

    class PrepareResponseContextBehavior : HandlingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var messageHandler = context.MessageHandler;
            // For backwards compat let's treat responses like messages
            if (messageHandler.HandlerKind == HandlerKind.Message)
            {
                context.Set("InvocationContext", new ResponseContext(context.Builder.Build<IBus>()));
            }

            next();
        }
    }
}