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
    public abstract class ForkConnector<TFromContext, TForkContext> : IBehavior<TFromContext, TFromContext>, IForkConnector<TFromContext, TFromContext, TForkContext>
        where TFromContext : IBehaviorContext
        where TForkContext : IBehaviorContext
    {
        /// <summary>
        /// Called when the fork connector is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="IBehavior{TFromContext,TFromContext}" /> in the chain to execute.</param>
        /// <param name="fork">The next <see cref="IBehavior{TForkContext,TForkContext}" /> in the chain to fork and execute.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe while invoking.</param>
        public abstract Task Invoke(TFromContext context, Func<TFromContext, CancellationToken, Task> next, Func<TForkContext, CancellationToken, Task> fork, CancellationToken token);

        /// <inheritdoc />
        public Task Invoke(TFromContext context, Func<TFromContext, CancellationToken, Task> next, CancellationToken token)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(next), next);

            return Invoke(context, next, (ctx, token2) => ctx.InvokePipeline(token2), token);
        }
    }
}