namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;

    static class PipelineInvocationExtensions
    {
        public static Task InvokePipeline<TContext>(this TContext context) where TContext : IBehaviorContext
        {
            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<TContext>();
            return pipeline.Invoke(context);
        }
    }
}