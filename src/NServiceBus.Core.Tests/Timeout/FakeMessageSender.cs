namespace NServiceBus.Core.Tests.Timeout
{
    using NServiceBus.Transports;

    public class FakeMessageSender : ISendMessages
    {
        private volatile int messagesSent;

        public int MessagesSent
        {
            get { return messagesSent; }
            set { messagesSent = value; }
        }

        public void Send(OutgoingMessage message, TransportSendOptions sendOptions)
        {
            MessagesSent++;
        }
    }
}