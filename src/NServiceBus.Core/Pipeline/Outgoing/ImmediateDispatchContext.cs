namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;
    using Transports;

    /// <summary>
    /// Context for the immediate dispatch part of the pipeline.
    /// </summary>
    public class ImmediateDispatchContext : BehaviorContext
    {
        /// <summary>
        /// The operations to be dispatched to the transport.
        /// </summary>
        public IEnumerable<TransportOperation> Operations { get; private set; }

        /// <summary>
        /// Initialized the context with the operations to dispatch.
        /// </summary>
        /// <param name="operations">The operations.</param>
        /// <param name="parentContext">The parent context.</param>
        public ImmediateDispatchContext(IEnumerable<TransportOperation> operations, BehaviorContext parentContext)
            : base(parentContext)
        {
            Operations = operations;
        }

        //note: will be removed in a different pull
        internal void Replace(List<TransportOperation> operations)
        {
            Operations = operations;
        }
    }
}