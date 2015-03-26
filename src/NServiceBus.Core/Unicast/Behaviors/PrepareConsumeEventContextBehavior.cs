namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Behaviors;

    class PrepareConsumeEventContextBehavior : HandlingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var messageHandler = context.MessageHandler;
            if (messageHandler.HandlerKind == HandlerKind.Event)
            {
                context.Set("InvocationContext", new ConsumeEventContext());
            }

            next();
        }
    }
}