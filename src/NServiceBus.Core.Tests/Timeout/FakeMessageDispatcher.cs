using System.Threading;

namespace NServiceBus.Core.Tests.Timeout
{
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    public class FakeMessageDispatcher : IDispatchMessages
    {
        public int MessagesSent
        {
            get { return messagesSent; }
            set { messagesSent = value; }
        }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transportTransaction, CancellationToken cancellationToken)
        {
            MessagesSent += outgoingMessages.MulticastTransportOperations.Count + outgoingMessages.UnicastTransportOperations.Count;
            return Task.CompletedTask;
        }

        volatile int messagesSent;
    }
}