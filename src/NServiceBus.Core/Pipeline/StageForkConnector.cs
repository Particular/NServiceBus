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
        public Task Invoke(TFromContext context, Func<TToContext, CancellationToken, Task> next, CancellationToken token)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(next), next);

            return Invoke(context, ctx => next(ctx, token), ctx => ctx.InvokePipeline(token), token);
        }

        /// <summary>
        /// Called when the stage fork connector is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="stage">The next <see cref="IBehavior{TToContext,TToContext}" /> in the chain to stage and execute.</param>
        /// <param name="fork">The next <see cref="IBehavior{TForkContext,TForkContext}" /> in the chain to fork and execute.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe while invoking.</param>
        public abstract Task Invoke(TFromContext context, Func<TToContext, Task> stage, Func<TForkContext, Task> fork, CancellationToken token);
    }
}