namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Behaviors;

    class PrepareReplyContextBehavior : HandlingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var messageHandler = context.MessageHandler;
            // For backwards compat let's treat replies like messages
            if (messageHandler.HandlerKind == HandlerKind.Message)
            {
                context.Set("InvocationContext", new ReplyContext());
            }

            next();
        }
    }
}