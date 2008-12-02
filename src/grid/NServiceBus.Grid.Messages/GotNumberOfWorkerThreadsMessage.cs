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
        public int NumberOfWorkerThreads { get; set; }
    }
}
