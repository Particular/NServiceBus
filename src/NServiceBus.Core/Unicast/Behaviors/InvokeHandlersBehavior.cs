namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    class InvokeHandlersBehavior : IBehavior<IncomingContext>
    {
        public void Invoke(IncomingContext context, Action next)
        {
            var messageHandler = context.MessageHandler;

            messageHandler.Invocation(messageHandler.Instance, context.IncomingLogicalMessage.Instance);

            next();
        }
    }
}