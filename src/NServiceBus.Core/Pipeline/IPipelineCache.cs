namespace NServiceBus
{
    using NServiceBus.Pipeline;

    interface IPipelineCache
    {
        IPipeline<TContext> Pipeline<TContext>()
            where TContext : IBehaviorContext;
    }
}