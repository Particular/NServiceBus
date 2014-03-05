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

        public int MaximumConcurrencyLevel
        {
            get { return 1; }
        }

        public void ChangeNumberOfWorkerThreads(int targetNumberOfWorkerThreads)
        {
        }

        public void ChangeMaximumConcurrencyLevel(int maximumConcurrencyLevel)
        {
            
        }

        public void AbortHandlingCurrentMessage()
        {
           
        }

        public void Stop()
        {
        }

        public int NumberOfWorkerThreads
        {
            get { return MaximumConcurrencyLevel; }
        }

        public void ChangeMaximumMessageThroughputPerSecond(int maximumMessageThroughputPerSecond)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<TransportMessageReceivedEventArgs> TransportMessageReceived;
        public event EventHandler<StartedMessageProcessingEventArgs> StartedMessageProcessing;
        public event EventHandler<FinishedMessageProcessingEventArgs> FinishedMessageProcessing;

        public event EventHandler<FailedMessageProcessingEventArgs> FailedMessageProcessing;

        public void FakeMessageBeingProcessed(TransportMessage transportMessage)
        {
            StartedMessageProcessing(this, new StartedMessageProcessingEventArgs(transportMessage));
            TransportMessageReceived(this,new TransportMessageReceivedEventArgs(transportMessage));
            FinishedMessageProcessing(this, new FinishedMessageProcessingEventArgs(transportMessage));
        }

        public void FakeMessageBeingPassedToTheFaultManager(TransportMessage transportMessage)
        {
            try
            {
                StartedMessageProcessing(this, new StartedMessageProcessingEventArgs(transportMessage));
            }
            catch(Exception ex)
            {
                if (FailedMessageProcessing != null)
                    FailedMessageProcessing(this, new FailedMessageProcessingEventArgs(transportMessage, ex));
            }
            FinishedMessageProcessing(this, new FinishedMessageProcessingEventArgs(transportMessage));
        }
        /// <summary>
        /// todo: use for testing.
        /// </summary>
        public int MaxThroughputPerSecond { get; set; }

        public int MaximumMessageThroughputPerSecond { get; private set; }
    }
}