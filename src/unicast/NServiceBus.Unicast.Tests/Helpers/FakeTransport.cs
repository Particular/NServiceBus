namespace NServiceBus.Unicast.Tests.Helpers
{
    using System;
    using Transport;

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

        public void FakeMessageBeeingProcessed(TransportMessage transportMessage)
        {
            StartedMessageProcessing(this, new EventArgs());
            TransportMessageReceived(this,new TransportMessageReceivedEventArgs(transportMessage));
            FinishedMessageProcessing(this,new EventArgs());
        }

        public void FakeMessageBeeingPassedToTheFaultManager(TransportMessage transportMessage)
        {
            StartedMessageProcessing(this, new EventArgs());
            FinishedMessageProcessing(this, new EventArgs());
        }
    }
}