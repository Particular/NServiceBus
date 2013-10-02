namespace NServiceBus.Core.Tests.Fakes
{
    using System;
    using Transports;
    using Unicast.Transport;

    public class FakeReceiver : IDequeueMessages
    {
        public void FakeMessageReceived()
        {
            FakeMessageReceived(new TransportMessage());
        }

        public void FakeMessageReceived(TransportMessage message)
        {
            if (TryProcessMessage(message))
                NumberOfMessagesReceived++;
        }


        public void Init(Address address, TransactionSettings transactionSettings, Func<TransportMessage, bool> tryProcessMessage, Action<TransportMessage, Exception> endProcessMessage)
        {
            InputAddress = address;
            TryProcessMessage = tryProcessMessage;
        }

        public void Start(int maximumConcurrencyLevel)
        {
            IsStarted = true;
        }

        public void Stop()
        {
           
        }

        Func<TransportMessage, bool> TryProcessMessage;
        public int NumberOfMessagesReceived;

        public bool IsStarted { get; set; }

        public Address InputAddress { get; set; }
    }
}