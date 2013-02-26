namespace NServiceBus.Gateway.Tests
{
    using System.Collections.Generic;
    using Transports;

    internal class InMemoryReceiver : IReceiveMessages
    {
        private Queue<TransportMessage> queue;

        void IReceiveMessages.Init(Address address, bool transactional)
        {
            queue = new Queue<TransportMessage>();
        }

        public TransportMessage Receive()
        {

            lock (queue)
                return queue.Dequeue();
        }

        public void Add(TransportMessage message)
        {
            lock (queue)
                queue.Enqueue(message);
        }
    }
}