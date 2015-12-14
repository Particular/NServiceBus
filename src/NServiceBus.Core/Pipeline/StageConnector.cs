namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Unicast.Transport;

    /// <summary>
    /// Connects two stages of the pipeline.
    /// </summary>
    public abstract class StageConnector<TFrom, TTo> :IStageConnector, IBehavior<TFrom, TTo> 
        where TFrom : IBehaviorContext
        where TTo : IBehaviorContext
    {
        /// <summary>
        /// Contains information about the pipeline this behavior is part of.
        /// </summary>
        protected PipelineInfo PipelineInfo { get; private set; }

        /// <inheritdoc />
        public abstract Task Invoke(TFrom context, Func<TTo, Task> next);

        /// <inheritdoc />
        public void Initialize(PipelineInfo pipelineInfo)
        {
            PipelineInfo = pipelineInfo;
        }

        /// <inheritdoc />
        public virtual Task Warmup()
        {
            return TaskEx.Completed;
        }

        /// <inheritdoc />
        public virtual Task Cooldown()
        {
            return TaskEx.Completed;
        }
    }
}