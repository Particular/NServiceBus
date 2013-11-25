namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    class ChildContainerBehavior:IBehavior<PhysicalMessageContext>
    {
        public void Invoke(PhysicalMessageContext context, Action next)
        {
            using (var childBuilder = context.Builder.CreateChildBuilder())
            {
                context.Set(childBuilder);
                next();
            }
        }
    }
}