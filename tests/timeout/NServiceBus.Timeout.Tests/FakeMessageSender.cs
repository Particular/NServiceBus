namespace NServiceBus.Timeout.Tests
{
    using Unicast.Queuing;

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