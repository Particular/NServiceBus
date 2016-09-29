namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;
    using Transport;

    class DispatchContext : BehaviorContext, IDispatchContext
    {
        public DispatchContext(IReadOnlyCollection<TransportOperation> operations, IBehaviorContext parentContext)
            : base(parentContext)
        {
            Operations = operations;
        }

        public IEnumerable<TransportOperation> Operations { get; }
    }
}