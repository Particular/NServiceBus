namespace NServiceBus.Gateway.Tests
{
    using System.Threading;
    using Unicast.Queuing;
    using Unicast.Transport;

    public class FakeMessageSender:ISendMessages
    {
        public FakeMessageSender()
        {
            messageReceived = new ManualResetEvent(false);
            
        }
        public void Send(TransportMessage message, string destination)
        {

            receivedMessage = message;
            messageReceived.Set();
        }

        public TransportMessage GetResultingMessage()
        {
            messageReceived.WaitOne();
            return receivedMessage;
        }

        TransportMessage receivedMessage;
        readonly ManualResetEvent messageReceived;

    }
}