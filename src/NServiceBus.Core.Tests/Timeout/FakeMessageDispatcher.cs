namespace NServiceBus.Core.Tests.Timeout
{
    using System.Threading.Tasks;
    using NServiceBus.Transports;

    public class FakeMessageDispatcher : IDispatchMessages
    {
        volatile int messagesSent;

        public int MessagesSent
        {
            get { return messagesSent; }
            set { messagesSent = value; }
        }

        public Task Dispatch(OutgoingMessage message, DispatchOptions dispatchOptions)
        {
            MessagesSent++;
            return Task.FromResult(0);
        }
    }
}