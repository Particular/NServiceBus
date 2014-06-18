namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// This is the base interface to implement to create a behavior that can be registered in a pipeline.
    /// </summary>
    /// <typeparam name="TContext">The context that this behavior should receive.</typeparam>
    public interface IBehavior<in TContext> where TContext : BehaviorContext
    {
        /// <summary>
        /// Called when the behavior is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="IBehavior{TContext}"/> in the chain to execute.</param>
        void Invoke(TContext context, Action next);
    }
}