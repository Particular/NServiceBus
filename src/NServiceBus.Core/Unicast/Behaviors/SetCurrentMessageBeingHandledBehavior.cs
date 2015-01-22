namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline.Contexts;

    class SetCurrentMessageBeingHandledBehavior : HandlingStageBehavior
    {
        public override void Invoke(Context context, Action next)
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