namespace NServiceBus
{
    using System.Collections.Generic;
    using Transports;

    class PendingTransportOperations
    {
        public IReadOnlyCollection<TransportOperation> Operations => operations;

        public void Add(TransportOperation transportOperation)
        {
            operations.Add(transportOperation);
        }

        public void AddRange(IEnumerable<TransportOperation> transportOperations)
        {
            operations.AddRange(transportOperations);
        }

        List<TransportOperation> operations = new List<TransportOperation>();
    }
}