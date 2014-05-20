namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using ObjectBuilder;
    using Pipeline;
    using Pipeline.Contexts;

    class ChildContainerBehavior : IBehavior<IncomingContext>
    {
        public void Invoke(IncomingContext context, Action next)
        {
            using (var childBuilder = context.Builder.CreateChildBuilder())
            {
                context.Set(childBuilder);
                try
                {
                    next();
                }
                finally
                {
                    context.Remove<IBuilder>();
                }
            }
        }
    }
}