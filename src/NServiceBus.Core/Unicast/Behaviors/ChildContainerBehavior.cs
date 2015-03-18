namespace NServiceBus
{
    using System;
    using ObjectBuilder;

    class ChildContainerBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override void Invoke(Context context, Action next)
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