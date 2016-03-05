namespace NServiceBus
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Transports;

    class PendingTransportOperations
    {
        public IReadOnlyCollection<TransportOperation> Operations => new List<TransportOperation>(operations);

        public bool HasOperations => !operations.IsEmpty;

        public void Add(TransportOperation transportOperation)
        {
            operations.Push(transportOperation);
        }

        public void AddRange(IEnumerable<TransportOperation> transportOperations)
        {

            operations.PushRange(transportOperations.ToArray());
        }

        ConcurrentStack<TransportOperation> operations = new ConcurrentStack<TransportOperation>();
    }
}
