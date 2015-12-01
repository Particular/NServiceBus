namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class BatchDispatchContextImpl : BehaviorContextImpl, BatchDispatchContext
    {
        public BatchDispatchContextImpl(IReadOnlyCollection<TransportOperation> operations, BehaviorContext parentContext)
            : base(parentContext)
        {
            Operations = operations;
        }

        public IReadOnlyCollection<TransportOperation> Operations { get; }
    }
}