namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    static class PipelineInvocationExtensions
    {
        public static Task InvokePipeline<TContext>(this TContext context, CancellationToken token) where TContext : IBehaviorContext
        {
            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<TContext>();
            return pipeline.Invoke(context, token);
        }
    }
}