namespace NServiceBus.Transports
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Pipeline;
    using Transport;

    /// <summary>
    /// Allows the dispatch pipeline to be invoked for the two transport contexts.
    /// </summary>
    public static class TransportContextExtensions
    {
        /// <summary>
        /// Executes the dispatch pipeline from the given ErrorContext.
        /// </summary>
        public static Task Dispatch(this ErrorContext context, IReadOnlyCollection<TransportOperation> transportOperations, CancellationToken cancellationToken = default)
        {
            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<IDispatchContext>();
            return pipeline.Invoke(new DispatchContext(transportOperations, new ErrorContextWrapper(context)));
        }

        class ErrorContextWrapper : ContextBag, IBehaviorContext
        {
            public ErrorContextWrapper(ErrorContext errorContext, CancellationToken cancellationToken = default) : base(errorContext.Extensions)
            {
                CancellationToken = cancellationToken;
            }

            public ContextBag Extensions => this;
            public IServiceProvider Builder => Get<IServiceProvider>();

            public CancellationToken CancellationToken { get; }
        }
    }


}