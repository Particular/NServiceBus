namespace NServiceBus.Outbox
{
    using System.Collections.Generic;

    public class OutboxMessage
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
            set
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