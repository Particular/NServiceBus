namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class DispatchContextImpl : BehaviorContextImpl, DispatchContext
    {
        public DispatchContextImpl(IReadOnlyCollection<TransportOperation> operations, BehaviorContext parentContext)
            : base(parentContext)
        {
            Operations = operations;
        }

        public IEnumerable<TransportOperation> Operations { get; private set; }

        //note: will be removed in a different pull
        internal void Replace(List<TransportOperation> operations)
        {
            Operations = operations;
        }
    }
}