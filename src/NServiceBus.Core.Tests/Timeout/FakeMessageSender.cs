namespace NServiceBus.Core.Tests.Timeout
{
    using Transports;
    using Unicast;

    public class FakeMessageSender : ISendMessages
    {
        private volatile int messagesSent;

        public int MessagesSent
        {
            get { return messagesSent; }
            set { messagesSent = value; }
        }

        public void Send(TransportMessage message, SendOptions sendOptions)
        {
            MessagesSent++;
        }
    }
}