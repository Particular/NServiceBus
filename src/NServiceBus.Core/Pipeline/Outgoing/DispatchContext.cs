namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Context for the immediate dispatch part of the pipeline.
    /// </summary>
    public interface DispatchContext : BehaviorContext
    {
        /// <summary>
        /// The operations to be dispatched to the transport.
        /// </summary>
        IEnumerable<TransportOperation> Operations { get; }
    }
}