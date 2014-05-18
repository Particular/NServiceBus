namespace NServiceBus.Pipeline
{
    using System;

    public interface IBehavior<in TContext> where TContext : BehaviorContext
    {
        void Invoke(TContext context, Action next);
    }

}