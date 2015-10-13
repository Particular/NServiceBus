namespace NServiceBus.Core.Tests.Timeout
{
    using System.Collections.Generic;
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

        public Task Dispatch(IEnumerable<TransportOperation> outgoingMessages, ContextBag context)
        {
            MessagesSent += outgoingMessages.Count();
            return Task.FromResult(0);
        }

        volatile int messagesSent;
    }
}