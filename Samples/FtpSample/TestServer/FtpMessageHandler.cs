using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus;
using TestMessage;

namespace TestServer
{
    public class FtpMessageHandler : IMessageHandler<TestMessage.FtpMessage>
    {
        public IBus Bus { get; set; }

        #region IMessageHandler<FtpMessage> Members

        public void Handle(TestMessage.FtpMessage message)
        {
            Console.WriteLine("Message Received With The Following\n\n");

            Console.WriteLine("ID: " + message.ID.ToString());
            Console.WriteLine("Name: " + message.Name);

            FtpReply rep = new FtpReply { ID = 500, OtherData = Guid.NewGuid(), IsThisCool = true, Description = "What the?" };
            this.Bus.Reply(rep);
        }

        #endregion
    }
}
