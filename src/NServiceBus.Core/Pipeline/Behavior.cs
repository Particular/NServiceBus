namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// This is the base interface to implement to create a behavior that can be registered in a pipeline.
    /// </summary>
    /// <typeparam name="TIn">The context that this behavior should receive.</typeparam>
    /// <typeparam name="TOut"></typeparam>
    public interface IBehavior<in TIn, out TOut>
        where TIn : BehaviorContext
        where TOut : BehaviorContext
    {
        /// <summary>
        /// Called when the behavior is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="IBehavior{TIn,TOut}"/> in the chain to execute.</param>
        void Invoke(TIn context, Action<TOut> next);
    }

  
    /// <summary>
    /// This is the base interface to implement to create a behavior that can be registered in a pipeline.
    /// </summary>
    /// <typeparam name="TContext">The context that this behavior should receive.</typeparam>
    public abstract class Behavior<TContext> : IBehavior<TContext,TContext> where TContext : BehaviorContext
    {
        /// <summary>
        /// Called when the behavior is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="!:IBehavior{TContext}" /> in the chain to execute.</param>
        public abstract void Invoke(TContext context, Action next);

        /// <summary>
        /// Called when the behavior is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="IBehavior{TIn,TOut}"/> in the chain to execute.</param>
        public void Invoke(TContext context, Action<TContext> next)
        {
            Guard.AgainstNull(context, "context");
            Guard.AgainstNull(next, "next");
            Invoke(context, () => next(context));
        }
    }
}