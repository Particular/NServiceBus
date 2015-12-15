namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    interface IPipeline
    {
    }

    interface IPipeline<in TContext> : IPipeline
        where TContext : IBehaviorContext
    {
        Task Invoke(TContext context);
    }
}