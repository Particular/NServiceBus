namespace NServiceBus.Unicast.Transport.Transactional
{
    using System;

    public interface IDequeueMessages
    {
        void Init(Address address, TransactionSettings transactionSettings);
        
        void Start(int maxDegreeOfParallelism);
        
        void ChangeMaxDegreeOfParallelism(int value);

        void Stop();

        event EventHandler<TransportMessageAvailableEventArgs> MessageDequeued; 
    }

    public class TransportMessageAvailableEventArgs : EventArgs
    {
        public TransportMessageAvailableEventArgs(TransportMessage m)
        {
            message = m;
        }

        readonly TransportMessage message;

        public TransportMessage Message
        {
            get { return message; }
        }
    }
}