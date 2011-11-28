namespace NServiceBus.Unicast.Tests.Helpers
{
    using System;
    using NServiceBus.Unicast.Transport;

    public class FakeTransport : ITransport
    {
        public void Dispose()
        {
           
        }

        public void Start(string inputqueue)
        {
        }

        public void Start(Address localAddress)
        {
        }

        public void ChangeNumberOfWorkerThreads(int targetNumberOfWorkerThreads)
        {
        }

        public void AbortHandlingCurrentMessage()
        {
           
        }

        public int NumberOfWorkerThreads
        {
            get { return 1; }
        }

        public event EventHandler<TransportMessageReceivedEventArgs> TransportMessageReceived;
        public event EventHandler StartedMessageProcessing;
        public event EventHandler FinishedMessageProcessing;
        public event EventHandler<FailedMessageProcessingEventArgs> FailedMessageProcessing;

        public void FakeTransportMessageReceived(TransportMessage transportMessage)
        {
            TransportMessageReceived(this,new TransportMessageReceivedEventArgs(transportMessage));
        }
    }
}