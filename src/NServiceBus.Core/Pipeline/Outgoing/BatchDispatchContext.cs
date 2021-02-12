namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;
    using Transport;

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