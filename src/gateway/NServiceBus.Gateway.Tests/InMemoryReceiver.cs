namespace NServiceBus.Gateway.Tests
{
    using System.Collections.Generic;
    using Unicast.Queuing;
    using Unicast.Transport;

    internal class InMemoryReceiver:IReceiveMessages
    {
        Queue<TransportMessage> queue;

        void IReceiveMessages.Init(Address address, bool transactional)
        {
            queue = new Queue<TransportMessage>();
        }

        public bool HasMessage()
        {
            lock (queue)
                return queue.Count > 0;
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