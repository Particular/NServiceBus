namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Pipeline context for dispatching pending transport operations captured during message processing.
    /// </summary>
    public class BatchDispatchContext : BehaviorContext, IBatchDispatchContext
    {
        /// <summary>
        /// Creates a new instance of a batch dispatch context.
        /// </summary>
        /// <param name="operations">The captured transport operations to dispatch.</param>
        /// <param name="parentContext">The parent context.</param>
        public BatchDispatchContext(IReadOnlyCollection<TransportOperation> operations, IBehaviorContext parentContext)
            : base(parentContext)
        {
            Operations = operations;
        }

        /// <summary>
        /// The captured transport operations to dispatch.
        /// </summary>
        public IReadOnlyCollection<TransportOperation> Operations { get; }
    }
}