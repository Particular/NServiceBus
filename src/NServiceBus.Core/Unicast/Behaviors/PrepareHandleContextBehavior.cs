namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Behaviors;

    class PrepareHandleContextBehavior : HandlingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var messageHandler = context.MessageHandler;
            if (messageHandler.HandlerKind == HandlerKind.Message)
            {
                context.Set("InvocationContext", new HandleContext());
            }

            next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("PrepareHandleContext", typeof(PrepareHandleContextBehavior), "If the current handler handles messages a handle context is added as invocation context.")
            {
                InsertBeforeIfExists(WellKnownStep.InvokeHandlers);
            }
        }
    }
}