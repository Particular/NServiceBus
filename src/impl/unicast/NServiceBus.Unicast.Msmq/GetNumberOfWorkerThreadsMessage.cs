
using System;
namespace NServiceBus.Unicast.Transport.Msmq
{
    /// <summary>
    /// Request message sent to an <see cref="MsmqTransport"/> to get
    /// the number of worker threads. Response is a <see cref="GotNumberOfWorkerThreadsMessage"/>.
    /// </summary>
    [Serializable]
    public class GetNumberOfWorkerThreadsMessage : IMessage
    {
    }
}
