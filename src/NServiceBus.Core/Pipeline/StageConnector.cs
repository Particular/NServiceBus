namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Unicast.Transport;

    /// <summary>
    /// Connects two stages of the pipeline.
    /// </summary>
    public abstract class StageConnector<TFrom, TTo> :IStageConnector, IBehavior<TFrom, TTo> 
        where TFrom : BehaviorContext
        where TTo : BehaviorContext
    {
        /// <summary>
        /// Contains information about the pipeline this behavior is part of.
        /// </summary>
        protected PipelineInfo PipelineInfo { get; private set; }

        /// <inheritdoc />
        public abstract void Invoke(TFrom context, Action<TTo> next);

        /// <inheritdoc />
        public void Initialize(PipelineInfo pipelineInfo)
        {
            PipelineInfo = pipelineInfo;
        }

        /// <inheritdoc />
        public virtual Task Warmup()
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public virtual Task Cooldown()
        {
            return Task.FromResult(true);
        }
    }
}