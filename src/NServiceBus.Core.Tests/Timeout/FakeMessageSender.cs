namespace NServiceBus.Core.Tests.Timeout
{
    using Transports;

    public class FakeMessageSender : ISendMessages
    {
        private volatile int messagesSent;

        public int MessagesSent
        {
            get { return messagesSent; }
            set { messagesSent = value; }
        }

        public void Send(TransportMessage message, Address address)
        {
            MessagesSent++;
        }
    }
}