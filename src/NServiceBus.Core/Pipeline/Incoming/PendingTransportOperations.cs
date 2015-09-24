namespace NServiceBus
{
    using System.Collections.Generic;
    using Transports;

    class PendingTransportOperations
    {  
        public IEnumerable<TransportOperation> Operations { get { return operations; } }

        public void Add(TransportOperation transportOperation)
        {
           operations.Add(transportOperation);
        }

        List<TransportOperation>  operations = new List<TransportOperation>();   
    }
}