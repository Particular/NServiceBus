namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Unicast.Transport;

    /// <summary>
    /// Connects two stages of the pipeline 
    /// </summary>
    /// <typeparam name="TFrom"></typeparam>
    /// <typeparam name="TTo"></typeparam>
    public abstract class StageConnector<TFrom, TTo> :IStageConnector, IBehavior<TFrom, TTo> 
        where TFrom : BehaviorContext
        where TTo : BehaviorContext
    {
        /// <summary>
        /// Contains information about the pipeline this behavior is part of.
        /// </summary>
        protected PipelineInfo PipelineInfo { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        public abstract void Invoke(TFrom context, Action<TTo> next);

        /// <summary>
        /// Initialized the behavior with information about the just constructed pipeline.
        /// </summary>
        /// <param name="pipelineInfo"></param>
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

    /// <summary>
    /// 
    /// </summary>
    public interface IStageConnector
    {
        
    }
}