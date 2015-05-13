namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Unicast.Transport;

    /// <summary>
    /// Base interface for all behaviors.
    /// </summary>
    public interface IBehavior
    {

        /// <summary>
        /// Initialized the behavior with information about the just constructed pipeline.
        /// </summary>
        void Initialize(PipelineInfo pipelineInfo);

        /// <summary>
        /// Allows a behavior to perform any necessary warm-up activities (such as priming a cache), possibly in an async way.
        /// </summary>
        Task Warmup();

        /// <summary>
        /// Allows a behavior to perform any necessary cool-down activities, possibly in an async way.
        /// </summary>
        Task Cooldown();
    }

    /// <summary>
    /// This is the base interface to implement to create a behavior that can be registered in a pipeline.
    /// </summary>
    /// <typeparam name="TIn">The type of context that this behavior should receive.</typeparam>
    /// <typeparam name="TOut">The type of context that this behavior should output.</typeparam>
    public interface IBehavior<in TIn, out TOut> : IBehavior
        where TIn : BehaviorContext
        where TOut : BehaviorContext
    {
        /// <summary>
        /// Called when the behavior is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="IBehavior{TIn,TOut}"/> in the chain to execute.</param>
        Task Invoke(TIn context, Func<TOut, Task> next);
    }

    /// <summary>
    /// This is the base interface to implement to create a <see cref="IBehavior"/> that can be registered in a pipeline.
    /// </summary>
    /// <typeparam name="TContext">The context that this <see cref="IBehavior"/> should receive.</typeparam>
    public abstract class Behavior<TContext> : IBehavior<TContext, TContext> where TContext : BehaviorContext
    {
        /// <summary>
        /// Contains information about the pipeline this behavior is part of.
        /// </summary>
        protected PipelineInfo PipelineInfo { get; private set; }

        /// <summary>
        /// Called when the behavior is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="!:IBehavior{TContext}" /> in the chain to execute.</param>
        public abstract Task Invoke(TContext context, Func<Task> next);

        /// <summary>
        /// Called when the behavior is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="IBehavior{TIn,TOut}"/> in the chain to execute.</param>
        public Task Invoke(TContext context, Func<TContext, Task> next)
        {
            Guard.AgainstNull("context", context);
            Guard.AgainstNull("next", next);
            return Invoke(context, () => next(context));
        }

        /// <summary>
        /// Initialized the behavior with information about the just constructed pipeline.
        /// </summary>
        public void Initialize(PipelineInfo pipelineInfo)
        {
            PipelineInfo = pipelineInfo;
        }

        /// <summary>
        /// Allows a behavior to perform any necessary warm-up activities (such as priming a cache), possibly in an async way.
        /// </summary>
        public virtual Task Warmup()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Allows a behavior to perform any necessary cool-down activities, possibly in an async way.
        /// </summary>
        public virtual Task Cooldown()
        {
            return Task.FromResult(true);
        }
    }
}