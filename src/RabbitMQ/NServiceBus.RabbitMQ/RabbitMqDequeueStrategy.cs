namespace NServiceBus.RabbitMQ
{
    using System;
    using Unicast.Transport.Transactional;

    public class RabbitMqDequeueStrategy : IDequeueMessages
    {
        public void Init(Address address, TransactionSettings transactionSettings, Func<bool> commitTransation)
        {
            throw new NotImplementedException();
        }

        public void Start(int maximumConcurrencyLevel)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<TransportMessageAvailableEventArgs> MessageDequeued;
    }
}