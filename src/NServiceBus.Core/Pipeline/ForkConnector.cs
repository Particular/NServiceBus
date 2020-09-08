namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Forks into another independent pipeline.
    /// </summary>
    /// <typeparam name="TFromContext">The context to connect from.</typeparam>
    /// <typeparam name="TForkContext">The context to fork an independent pipeline to.</typeparam>
    public abstract class ForkConnector<TFromContext, TForkContext> : Behavior<TFromContext>, IForkConnector<TFromContext, TFromContext, TForkContext>
        where TFromContext : IBehaviorContext
        where TForkContext : IBehaviorContext
    {
        /// <inheritdoc />
        public abstract Task Invoke(TFromContext context, Func<CancellationToken, Task> next, Func<TForkContext, CancellationToken, Task> fork, CancellationToken cancellationToken);

        /// <inheritdoc />
        public sealed override Task Invoke(TFromContext context, Func<CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(next), next);

            return Invoke(context, next, (ctx,ct) => ctx.InvokePipeline(ct), cancellationToken);
        }
    }
}