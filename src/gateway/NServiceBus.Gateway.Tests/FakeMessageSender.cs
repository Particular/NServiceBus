namespace NServiceBus.Gateway.Tests
{
    using System;
    using System.Threading;
    using Unicast.Queuing;
    using Unicast.Transport;

    public class FakeMessageSender:ISendMessages
    {
        public FakeMessageSender()
        {
            messageReceived = new ManualResetEvent(false);
        }

        void ISendMessages.Send(TransportMessage message, string destination)
        {
            ((ISendMessages)this).Send(message, Address.Parse(destination));
        }

        void ISendMessages.Send(TransportMessage message, Address address)
        {
            details = new SendDetails
            {
                Destination = address,
                Message = message
            };

            messageReceived.Set();
        }

        public SendDetails GetResultingMessage()
        {
            messageReceived.WaitOne(TimeSpan.FromSeconds(10));
            return details;
        }

        SendDetails details;
        readonly ManualResetEvent messageReceived;

        public class SendDetails
        {
            public TransportMessage Message { get; set; }
            public Address Destination { get; set; }
        }
    }
}