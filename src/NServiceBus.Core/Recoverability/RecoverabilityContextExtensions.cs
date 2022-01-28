namespace NServiceBus.Recoverability
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using Transport;

    /// <summary>
    /// Allows the dispatch pipeline to be invoked from the recoverability pipeline.
    /// </summary>
    public static class RecoverabilityContextExtensions
    {
        /// <summary>
        /// Executes the dispatch pipeline.
        /// </summary>
        public static Task Dispatch(this IRecoverabilityContext context, IReadOnlyCollection<TransportOperation> transportOperations)
        {
            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<IDispatchContext>();
            return pipeline.Invoke(new DispatchContext(transportOperations, context));
        }
    }
}