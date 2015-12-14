namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Context for the immediate dispatch part of the pipeline.
    /// </summary>
    public class DispatchContext : BehaviorContext, IDispatchContext
    {
        /// <summary>
        /// Creates a new instance of a dispatch context.
        /// </summary>
        /// <param name="operations">The operations to be dispatched to the transport.</param>
        /// <param name="parentContext">The parent context.</param>
        public DispatchContext(IReadOnlyCollection<TransportOperation> operations, IBehaviorContext parentContext)
            : base(parentContext)
        {
            Operations = operations;
        }

        /// <summary>
        /// The operations to be dispatched to the transport.
        /// </summary>
        public IEnumerable<TransportOperation> Operations { get; private set; }

        //note: will be removed in a different pull
        internal void Replace(List<TransportOperation> operations)
        {
            Operations = operations;
        }
    }
}