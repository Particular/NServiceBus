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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public abstract Task Invoke(TFromContext context, Func<Task> next, Func<TForkContext, Task> fork);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc />
        public sealed override Task Invoke(TFromContext context, Func<Task> next)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(next), next);

            return Invoke(context, next, ctx => ctx.InvokePipeline());
        }
    }
}