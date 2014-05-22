namespace NServiceBus.Gateway.Tests
{
    using System;
    using System.Threading;
    using Transports;
    using Unicast;

    public class FakeMessageSender : ISendMessages
    {
        public FakeMessageSender()
        {
            messageReceived = new ManualResetEvent(false);
        }

        void ISendMessages.Send(TransportMessage message, SendOptions sendOptions)
        {
            details = new SendDetails
            {
                Destination = sendOptions.Destination,
                Message = message
            };

            messageReceived.Set();
        }

        public SendDetails GetResultingMessage()
        {
            messageReceived.WaitOne(TimeSpan.FromSeconds(200));
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