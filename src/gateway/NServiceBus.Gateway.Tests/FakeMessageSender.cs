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

            details = new SendDetails
                          {
                              Destination = destination,
                              Message = message
                          };

            messageReceived.Set();
        }

        public SendDetails GetResultingMessage()
        {
            messageReceived.WaitOne();
            return details;
        }

        SendDetails details;
        readonly ManualResetEvent messageReceived;

        public class SendDetails
        {
            public TransportMessage Message { get; set; }
            public string Destination { get; set; }
        }
    }
}