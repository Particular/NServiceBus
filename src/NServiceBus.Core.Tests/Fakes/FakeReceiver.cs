namespace NServiceBus.Core.Tests.Fakes
{
    using System;
    using NServiceBus.Unicast.Transport.Transactional;

    public class FakeReceiver : IDequeueMessages
    {
        public void FakeMessageReceived()
        {
            var tm = new TransportMessage
                {
                    Id = Guid.NewGuid().ToString()
                };
            MessageDequeued(this,new TransportMessageAvailableEventArgs(tm));

            NumMessagesReceived++;
        }

        public void Init(Address address, TransactionSettings transactionSettings)
        {
            
        }

        public void Start(int maxDegreeOfParallelism)
        {
            
        }

        public void Stop()
        {
           
        }
        public int NumMessagesReceived;
        public event EventHandler<TransportMessageAvailableEventArgs> MessageDequeued;
    }
}