namespace NServiceBus.Pipeline
{
    using System;
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
        public abstract Task Invoke(TFromContext context, Func<Task> next, Func<TForkContext, Task> fork);

        /// <inheritdoc />
        public sealed override Task Invoke(TFromContext context, Func<Task> next)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(next), next);

            return Invoke(context, next, ctx =>
            {
                var cache = ctx.Extensions.Get<IPipelineCache>();
                var pipeline = cache.Pipeline<TForkContext>();
                return pipeline.Invoke(ctx);
            });
        }
    }
}