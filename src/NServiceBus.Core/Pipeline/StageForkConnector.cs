namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Connects two stages of a pipeline and forks into an independent pipeline.
    /// </summary>
    /// <typeparam name="TFromContext">The context to connect from.</typeparam>
    /// <typeparam name="TToContext">The context to connect to.</typeparam>
    /// <typeparam name="TForkContext">The context to fork an independent pipeline to.</typeparam>
    public abstract class StageForkConnector<TFromContext, TToContext, TForkContext> : IStageForkConnector<TFromContext, TToContext, TForkContext>
        where TFromContext : IBehaviorContext
        where TToContext : IBehaviorContext
        where TForkContext : IBehaviorContext
    {
        /// <inheritdoc />
        public Task Invoke(TFromContext context, Func<TToContext, Task> next)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(next), next);

            return Invoke(context, next, ctx => ctx.InvokePipeline());
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public abstract Task Invoke(TFromContext context, Func<TToContext, Task> stage, Func<TForkContext, Task> fork);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}