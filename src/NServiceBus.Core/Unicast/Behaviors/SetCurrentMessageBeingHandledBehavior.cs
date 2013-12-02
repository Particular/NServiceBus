namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Messages;
    using Pipeline;
    using Pipeline.Contexts;

    class SetCurrentMessageBeingHandledBehavior : IBehavior<HandlerInvocationContext>
    {
        public void Invoke(HandlerInvocationContext context, Action next)
        {
            var logicalMessage = context.Get<LogicalMessage>();

            try
            {
                ExtensionMethods.CurrentMessageBeingHandled = logicalMessage.Instance;

                next();
            }
            finally
            {
                ExtensionMethods.CurrentMessageBeingHandled = null;
            }
        }
    }
}