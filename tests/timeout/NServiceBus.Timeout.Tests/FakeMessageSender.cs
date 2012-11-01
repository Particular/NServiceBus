namespace NServiceBus.Timeout.Tests
{
    using Unicast.Queuing;
    using Unicast.Transport;

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