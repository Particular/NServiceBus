namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// This is the base interface to implement to create a <see cref="IBehavior" /> that can be registered in a pipeline.
    /// </summary>
    /// <typeparam name="TContext">The context that this <see cref="IBehavior" /> should receive.</typeparam>
    public abstract class Behavior<TContext> : IBehavior<TContext, TContext> where TContext : IBehaviorContext
    {
        /// <summary>
        /// Called when the behavior is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="IBehavior{TIn,TOut}" /> in the chain to execute.</param>
        public Task Invoke(TContext context, Func<TContext, Task> next)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(next), next);
            return Invoke(context, () => next(context));
        }

        /// <summary>
        /// Called when the behavior is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="!:IBehavior{TContext}" /> in the chain to execute.</param>
        public abstract Task Invoke(TContext context, Func<Task> next);
    }
}