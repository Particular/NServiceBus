namespace NServiceBus.Gateway.Tests
{
    using System;
    using Unicast.Transport;

    public class FakeTransport : ITransport
    {
        public void Dispose()
        {
        }

        public void Stop()
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

        public int HasChangedMaximumConcurrencyLevelNTimes { get; set; }

        public int MaximumConcurrencyLevel { get; private set; }

        public void ChangeNumberOfWorkerThreads(int targetNumberOfWorkerThreads)
        {
            ChangeMaximumConcurrencyLevel(targetNumberOfWorkerThreads);
        }

        public void ChangeMaximumConcurrencyLevel(int maximumConcurrencyLevel)
        {
            MaximumConcurrencyLevel = maximumConcurrencyLevel;
            HasChangedMaximumConcurrencyLevelNTimes++;
        }

        public void AbortHandlingCurrentMessage()
        {
            throw new NotImplementedException();
        }

        public int NumberOfWorkerThreads { get; private set; }

        public int MaxThroughputPerSecond { get; set; }

        public int MaximumMessageThroughputPerSecond { get; private set; }

        public void RaiseEvent(TransportMessage message)
        {
            TransportMessageReceived(this, new TransportMessageReceivedEventArgs(message));
        }

        public void ChangeMaximumMessageThroughputPerSecond(int maximumMessageThroughputPerSecond)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<TransportMessageReceivedEventArgs> TransportMessageReceived;
        public event EventHandler<StartedMessageProcessingEventArgs> StartedMessageProcessing;
        public event EventHandler FinishedMessageProcessing;
        public event EventHandler<FailedMessageProcessingEventArgs> FailedMessageProcessing;
    }
}