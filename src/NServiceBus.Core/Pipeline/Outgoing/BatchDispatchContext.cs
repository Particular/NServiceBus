namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Pipeline context for dispatching pending transport operations captured during message processing.
    /// </summary>
    public interface BatchDispatchContext : BehaviorContext
    {
        /// <summary>
        /// The captured transport operations to dispatch.
        /// </summary>
        IReadOnlyCollection<TransportOperation> Operations { get; }
    }

    /// <summary>
    /// Pipeline context for dispatching pending transport operations captured during message processing.
    /// </summary>
    public class BatchDispatchContextImpl : BehaviorContextImpl, BatchDispatchContext
    {
        /// <summary>
        /// The captured transport operations to dispatch.
        /// </summary>
        public IReadOnlyCollection<TransportOperation> Operations { get; private set; }

        /// <summary>
        /// Create a new batch dispatch context.
        /// </summary>
        /// <param name="operations">The operations to dispatch.</param>
        /// <param name="parentContext">The parent receive context.</param>
        public BatchDispatchContextImpl(IReadOnlyCollection<TransportOperation> operations, BehaviorContext parentContext)
            : base(parentContext)
        {
            Operations = operations;
        }
    }
}