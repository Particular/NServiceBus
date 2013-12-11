namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using Messages;
    using Pipeline;
    using Pipeline.Contexts;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SetCurrentMessageBeingHandledBehavior : IBehavior<HandlerInvocationContext>
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