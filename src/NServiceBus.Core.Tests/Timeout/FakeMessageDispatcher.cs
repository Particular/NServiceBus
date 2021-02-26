namespace NServiceBus.Core.Tests.Timeout
{
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    public class FakeMessageDispatcher : IMessageDispatcher
    {
        public int MessagesSent
        {
            get { return messagesSent; }
            set { messagesSent = value; }
        }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transportTransaction, CancellationToken cancellationToken = default)
        {
            MessagesSent += outgoingMessages.MulticastTransportOperations.Count + outgoingMessages.UnicastTransportOperations.Count;
            return Task.CompletedTask;
        }

        volatile int messagesSent;
    }
}