namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Connects two stages of the pipeline.
    /// </summary>
    public abstract class StageConnector<TFromContext, TToContext> : IStageConnector<TFromContext, TToContext>
        where TFromContext : IBehaviorContext
        where TToContext : IBehaviorContext
    {
        /// <summary>
        /// Contains information about the pipeline this behavior is part of.
        /// </summary>
        /// <inheritdoc />
        public abstract Task Invoke(TFromContext context, Func<TToContext, Task> stage);
    }
}