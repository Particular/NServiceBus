namespace NServiceBus.Core.Tests.Timeout
{
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Transports;

    public class FakeMessageDispatcher : IDispatchMessages
    {
        public int MessagesSent
        {
            get { return messagesSent; }
            set { messagesSent = value; }
        }

        public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
        {
            MessagesSent += outgoingMessages.MulticastTransportOperations.Count() + outgoingMessages.UnicastTransportOperations.Count();
            return Task.FromResult(0);
        }

        volatile int messagesSent;
    }
}