namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using Transport;

    /// <summary>
    /// Context for the immediate dispatch part of the pipeline.
    /// </summary>
    public interface IDispatchContext : IBehaviorContext
    {
        /// <summary>
        /// The operations to be dispatched to the transport.
        /// </summary>
        IEnumerable<TransportOperation> Operations { get; }
    }
}