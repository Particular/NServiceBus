using System;

namespace NServiceBus.Unicast.Transport.Msmq
{
    /// <summary>
    /// Response message returned by the <see cref="MsmqTransport"/>
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
