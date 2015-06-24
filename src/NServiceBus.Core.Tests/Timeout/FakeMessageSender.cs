namespace NServiceBus.Core.Tests.Timeout
{
    using NServiceBus.Transports;

    public class FakeMessageSender : IDispatchMessages
    {
        private volatile int messagesSent;

        public int MessagesSent
        {
            get { return messagesSent; }
            set { messagesSent = value; }
        }

        public void Dispatch(OutgoingMessage message, DispatchOptions dispatchOptions)
        {
            MessagesSent++;
        }
    }
}