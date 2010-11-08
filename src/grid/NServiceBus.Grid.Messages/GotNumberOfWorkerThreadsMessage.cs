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
        /// <summary>
        /// The number of worker threads running on the sending endpoint.
        /// </summary>
        public int NumberOfWorkerThreads { get; set; }
    }
}
