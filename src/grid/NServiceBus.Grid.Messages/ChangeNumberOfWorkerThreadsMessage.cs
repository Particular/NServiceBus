using System;

namespace NServiceBus.Grid.Messages
{
    /// <summary>
    /// Message sent to request that the bus change the number of
    /// worker threads handling messages. 
    /// </summary>
    [Serializable]
    public class ChangeNumberOfWorkerThreadsMessage : IMessage
    {
        public ChangeNumberOfWorkerThreadsMessage() {}

        public ChangeNumberOfWorkerThreadsMessage(int numberOfWorkerThreads)
        {
            this.numberOfWorkerThreads = numberOfWorkerThreads;
        }

        private readonly int numberOfWorkerThreads;

        public int NumberOfWorkerThreads
        {
            get { return this.numberOfWorkerThreads;  }
        }
    }
}
