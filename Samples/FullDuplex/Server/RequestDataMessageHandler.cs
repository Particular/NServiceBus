using System;
using Messages;
using NServiceBus;

namespace Server
{
    public class RequestDataMessageHandler : IMessageHandler<RequestDataMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(RequestDataMessage message)
        {
            Console.WriteLine("Received request {0}.", message.DataId);
            Console.WriteLine("String received: {0}.", message.String);
            Console.Out.WriteLine("Header 'Test' = {0}.", Bus.IncomingHeaders["Test"]);

            DataResponseMessage response = new DataResponseMessage();
            response.DataId = message.DataId;
            response.String = message.String;

            Bus.OutgoingHeaders["Test"] = "server1111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111";
            Bus.OutgoingHeaders["1"] = "1";
            Bus.OutgoingHeaders["2"] = "2";

            this.Bus.Reply(response);

        }
    }
}
