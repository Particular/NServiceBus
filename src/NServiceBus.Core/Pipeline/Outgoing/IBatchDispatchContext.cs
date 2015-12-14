namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Pipeline context for dispatching pending transport operations captured during message processing.
    /// </summary>
    public interface IBatchDispatchContext : IBehaviorContext
    {
        /// <summary>
        /// The captured transport operations to dispatch.
        /// </summary>
        IReadOnlyCollection<TransportOperation> Operations { get; }
    }
}