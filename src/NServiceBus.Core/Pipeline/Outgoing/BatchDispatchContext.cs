namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class BatchDispatchContext : BehaviorContext, IBatchDispatchContext
    {
        public BatchDispatchContext(IReadOnlyCollection<TransportOperation> operations, IBehaviorContext parentContext)
            : base(parentContext)
        {
            Operations = operations;
        }

        public IReadOnlyCollection<TransportOperation> Operations { get; }
    }
}