namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    class ChildContainerBehavior:IBehavior<IncomingPhysicalMessageContext>
    {
        public void Invoke(IncomingPhysicalMessageContext context, Action next)
        {
            using (var childBuilder = context.Builder.CreateChildBuilder())
            {
                context.Set(childBuilder);
                next();
            }
        }
    }
}