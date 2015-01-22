namespace NServiceBus.Pipeline
{
    using System;

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
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        public abstract void Invoke(TFrom context, Action<TTo> next);

    }

    /// <summary>
    /// 
    /// </summary>
    public interface IStageConnector
    {
        
    }
}