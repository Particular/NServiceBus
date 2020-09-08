namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading;
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
        public Task Invoke(TFromContext context, Func<TToContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(next), next);

            return Invoke(context, next, (ctx,ct) => ctx.InvokePipeline(ct), cancellationToken);
        }

        /// <inheritdoc />
        public abstract Task Invoke(TFromContext context, Func<TToContext, CancellationToken, Task> stage, Func<TForkContext, CancellationToken, Task> fork, CancellationToken cancellationToken);
    }
}