namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// This is the base interface to implement to create a behavior that can be registered in a pipeline.
    /// </summary>
    /// <typeparam name="TIn">The type of context that this behavior should receive.</typeparam>
    /// <typeparam name="TOut">The type of context that this behavior should output.</typeparam>
    public interface IBehavior<in TIn, out TOut> : IBehavior
        where TIn : IBehaviorContext
        where TOut : IBehaviorContext
    {
        /// <summary>
        /// Called when the behavior is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="IBehavior{TIn,TOut}"/> in the chain to execute.</param>
        Task Invoke(TIn context, Func<TOut, Task> next);
    }

    /// <summary>
    /// Base interface for all behaviors.
    /// </summary>
    public interface IBehavior
    {
    }
}