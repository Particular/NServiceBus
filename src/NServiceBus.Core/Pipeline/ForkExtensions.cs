namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;

    static class ForkExtensions
    {
        public static Task Fork<TFromContext, TToContext, TForkContext>(this IForkConnector<TFromContext, TToContext, TForkContext> forkConnector, TForkContext context)
            where TForkContext : IBehaviorContext
            where TFromContext : IBehaviorContext
            where TToContext : IBehaviorContext
        {
            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<TForkContext>();
            return pipeline.Invoke(context);
        }
    }
}