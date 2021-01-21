namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    static class ForkExtensions
    {
        public static Task Fork<TFromContext, TToContext, TForkContext>(this IForkConnector<TFromContext, TToContext, TForkContext> forkConnector, TForkContext context, CancellationToken token)
            where TForkContext : IBehaviorContext
            where TFromContext : IBehaviorContext
            where TToContext : IBehaviorContext
        {
            _ = forkConnector;

            return context.InvokePipeline(token);
        }
    }
}