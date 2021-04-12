namespace NServiceBus.Pipeline
{
    using System;
    using System.Diagnostics.CodeAnalysis;
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
        /// <summary>
        /// Called when the fork connector is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="IBehavior{TFromContext,TFromContext}" /> in the chain to execute.</param>
        /// <param name="fork">The next <see cref="IBehavior{TForkContext,TForkContext}" /> in the chain to fork and execute.</param>
        [SuppressMessage("Code", "PS0013:A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext", Justification = "Behaviors use a context that includes a cancellation token.")]
        public abstract Task Invoke(TFromContext context, Func<Task> next, Func<TForkContext, Task> fork);

        /// <inheritdoc />
        public sealed override Task Invoke(TFromContext context, Func<Task> next)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(next), next);

            return Invoke(context, next, ctx => ctx.InvokePipeline());
        }
    }
}
