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

        /// <summary>
        /// Called when the stage fork connector is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="stage">The next <see cref="!:IBehavior{TToContext}" /> in the chain to stage and execute.</param>
        /// <param name="fork">The next <see cref="!:IBehavior{TForkContext}" /> in the chain to fork and execute.</param>
        public abstract Task Invoke(TFromContext context, Func<TToContext, Task> stage, Func<TForkContext, Task> fork);
    }
}