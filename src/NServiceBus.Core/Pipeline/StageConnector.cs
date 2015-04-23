namespace NServiceBus.Pipeline
{
    using System;
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

    /// <summary>
    /// 
    /// </summary>
    public interface IStageConnector
    {
        
    }
}