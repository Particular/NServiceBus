namespace NServiceBus.Pipeline
{
    using System;

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