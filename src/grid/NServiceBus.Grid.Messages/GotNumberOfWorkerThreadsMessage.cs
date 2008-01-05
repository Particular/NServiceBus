using System;

namespace NServiceBus.Grid.Messages
{
    /// <summary>
    /// Response message returned by the bus
    /// as a result of a <see cref="GetNumberOfWorkerThreadsMessage"/>.
    /// </summary>
    [Serializable]
    public class GotNumberOfWorkerThreadsMessage : IMessage
    {
        public GotNumberOfWorkerThreadsMessage() {}

        public GotNumberOfWorkerThreadsMessage(int numberOfWorkerThreads)
        {
            this.numberOfWorkerThreads = numberOfWorkerThreads;
        }

        private readonly int numberOfWorkerThreads;

        public int NumberOfWorkerThreads
        {
            get { return this.numberOfWorkerThreads; }
        }
    }
}
