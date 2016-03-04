namespace NServiceBus
{
    using Pipeline;

    interface IPipelineCache
    {
        IPipeline<TContext> Pipeline<TContext>()
            where TContext : IBehaviorContext;
    }
}