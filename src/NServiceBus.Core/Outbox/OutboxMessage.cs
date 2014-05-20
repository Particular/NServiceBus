namespace NServiceBus.Outbox
{
    using System.Collections.Generic;

    public class OutboxMessage
    {
        public OutboxMessage(string messageId)
        {
            Id = messageId;
        }

        public string Id { get; private set; }
       
        public List<TransportOperation> TransportOperations
        {
            get
            {
                if (transportOperations == null)
                {
                    transportOperations = new List<TransportOperation>();
                }

                return transportOperations;
            }
        }

        List<TransportOperation> transportOperations;
    }
}