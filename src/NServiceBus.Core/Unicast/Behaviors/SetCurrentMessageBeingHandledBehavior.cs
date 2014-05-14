namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SetCurrentMessageBeingHandledBehavior : IBehavior<IncomingContext>
    {
        public void Invoke(IncomingContext context, Action next)
        {
            var logicalMessage = context.IncomingLogicalMessage;

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