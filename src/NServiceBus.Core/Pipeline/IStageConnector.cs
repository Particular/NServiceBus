namespace NServiceBus
{
    using Pipeline;

    interface IStageConnector
    {
    }

    interface IStageConnector<in TFromContext, out TToContext> : IBehavior<TFromContext, TToContext>, IStageConnector
        where TFromContext : IBehaviorContext
        where TToContext : IBehaviorContext
    {
    }

    interface IStageForkConnector<in TFromContext, out TToContext, TForkContext> : IForkConnector<TFromContext, TToContext, TForkContext>, IStageConnector
        where TForkContext : IBehaviorContext
        where TFromContext : IBehaviorContext
        where TToContext : IBehaviorContext
    {
    }
}