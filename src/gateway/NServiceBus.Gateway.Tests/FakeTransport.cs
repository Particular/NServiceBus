namespace NServiceBus.Gateway.Tests
{
    using System;
    using Unicast.Transport;

    public class FakeTransport : ITransport
    {
        public void Dispose()
        {
            
        }
        
        public void Start(string inputqueue)
        {
            Start(Address.Parse(inputqueue));
        }

        public bool IsStarted { get; set; }
        public Address InputAddress { get; set; }
        public void Start(Address localAddress)
        {
            IsStarted = true;
            InputAddress = localAddress;
        }

        public int HasChangedNumberOfThreadsNTimes { get; set; }
        public void ChangeNumberOfWorkerThreads(int targetNumberOfWorkerThreads)
        {
            NumberOfWorkerThreads = targetNumberOfWorkerThreads;
            HasChangedNumberOfThreadsNTimes++;
        }

        public void AbortHandlingCurrentMessage()
        {
            throw new NotImplementedException();
        }

        public int NumberOfWorkerThreads { get; set; }

        public int MaxThroughputPerSecond { get; set; }

        public bool IsEventAssiged
        {
            get { return TransportMessageReceived != null; }
        }

        public void RaiseEvent(TransportMessage message)
        {
            TransportMessageReceived(this, new TransportMessageReceivedEventArgs(message));
        }

        public event EventHandler<TransportMessageReceivedEventArgs> TransportMessageReceived;
        public event EventHandler<StartedMessageProcessingEventArgs> StartedMessageProcessing;
        public event EventHandler FinishedMessageProcessing;
        public event EventHandler<FailedMessageProcessingEventArgs> FailedMessageProcessing;
    }
}