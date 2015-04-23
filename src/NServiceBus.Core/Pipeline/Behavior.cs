namespace NServiceBus.Pipeline
{
    using System;
    using NServiceBus.Unicast.Transport;

    /// <summary>
    /// Base interface for all behaviors.
    /// </summary>
    public interface IBehavior
    {

        /// <summary>
        /// Initialized the behavior with information about the just constructed pipeline.
        /// </summary>
        /// <param name="pipelineInfo"></param>
        void Initialize(PipelineInfo pipelineInfo);

        /// <summary>
        /// Notifies the behavior that the pipeline it is part of has been constructed is going to start processing messages.
        /// </summary>
        void OnStarting();

        /// <summary>
        /// Notifies the behavior that the pipeline it is part of is going to stop processing messages.
        /// </summary>
        void OnStopped();
    }

    /// <summary>
    /// This is the base interface to implement to create a behavior that can be registered in a pipeline.
    /// </summary>
    /// <typeparam name="TIn">The context that this behavior should receive.</typeparam>
    /// <typeparam name="TOut"></typeparam>
    public interface IBehavior<in TIn, out TOut> : IBehavior
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

        /// <summary>
        /// Initialized the behavior with information about the just constructed pipeline.
        /// </summary>
        /// <param name="pipelineInfo"></param>
        public void Initialize(PipelineInfo pipelineInfo)
        {
            PipelineInfo = pipelineInfo;
        }

        /// <summary>
        /// Notifies the behavior that the pipeline it is part of has been constructed is going to start processing messages.
        /// </summary>
        public virtual void OnStarting()
        {
        }

        /// <summary>
        /// Notifies the behavior that the pipeline it is part of is going to stop processing messages.
        /// </summary>
        public virtual void OnStopped()
        {
        }
    }
}