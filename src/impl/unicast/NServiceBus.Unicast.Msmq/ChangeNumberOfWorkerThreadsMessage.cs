using System;
using Utils;

namespace NServiceBus.Unicast.Transport.Msmq
{
    /// <summary>
    /// Message sent to request that the <see cref="MsmqTransport"/> change the number of
    /// <see cref="WorkerThread" /> objects - actual threads that are dedicated to
    /// handling messages. 
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
