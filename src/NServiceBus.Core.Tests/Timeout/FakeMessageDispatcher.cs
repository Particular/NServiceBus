namespace NServiceBus.Core.Tests.Timeout
{
    using System.Linq;
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

        public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
        {
            MessagesSent += outgoingMessages.MulticastTransportOperations.Count() + outgoingMessages.UnicastTransportOperations.Count();
            return TaskEx.CompletedTask;
        }

        volatile int messagesSent;
    }
}