namespace NServiceBus.Core.Tests.Fakes
{
    using System;
    using Unicast.Transport.Transactional;

    public class FakeReceiver : IDequeueMessages
    {
        public void FakeMessageReceived()
        {
            var tm = new TransportMessage
                {
                    Id = Guid.NewGuid().ToString()
                };

            if (TryProcessMessage(tm))
                NumMessagesReceived++;
        }


        public void Init(Address address, TransactionSettings transactionSettings)
        {
            
        }

        public void Start(int maximumConcurrencyLevel)
        {
            
        }

        public void Stop()
        {
           
        }

        public Func<TransportMessage, bool> TryProcessMessage { get; set; }
        public int NumMessagesReceived;
    }
}