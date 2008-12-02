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
        public int NumberOfWorkerThreads { get; set; }
    }
}
