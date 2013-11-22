namespace NServiceBus.Core.Tests.Pipeline
{
    using System.Collections.Generic;

    class OutboxMessage
    {
        public string Id { get; set; }
       
        public bool IsDispatching { get { return isDispatching; } }

        public bool Dispatched { get; set; }
   
        public List<TransportOperation> TransportOperations
        {
            get
            {
                if (transportOperations == null)
                    transportOperations = new List<TransportOperation>();

                return transportOperations;
            }
            protected set
            {
                transportOperations = value;
            }
        }

        public void StartDispatching()
        {
            isDispatching = true;
        }

        List<TransportOperation> transportOperations;

        bool isDispatching;

        
    }
}