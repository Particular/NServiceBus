using System;
using MyMessages;
using NServiceBus;

namespace MyServer
{
    public class RequestDataMessageHandler : IHandleMessages<RequestDataMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(RequestDataMessage message)
        {
            Console.WriteLine("Received request {0}.", message.DataId);
            Console.WriteLine("String received: {0}.", message.String);
            Console.WriteLine("Header 'Test' = {0}.", message.GetHeader("Test"));

            var response = Bus.CreateInstance<DataResponseMessage>(m => 
            { 
                m.DataId = message.DataId;
                m.String = message.String;
            });

            response.CopyHeaderFromRequest("Test");
            response.SetHeader("1", "1");
            response.SetHeader("2", "2");

            Bus.Reply(response); //You can try experimenting with sending multiple replies
        }
    }
}
