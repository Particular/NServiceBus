using System;
using NServiceBus;
using TestMessage;

namespace TestServer
{
    public class FtpMessageHandler : IHandleMessages<FtpMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(FtpMessage message)
        {
            Console.WriteLine("Request received with id:" + message.Id);

            var rep = new FtpResponse { Id = 500, OtherData = Guid.NewGuid(), IsThisCool = true, Description = "What the?", ResponseId = message.Id };
            Bus.Reply(rep);
        }
    }
}
