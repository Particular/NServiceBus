using System;
using Messages;
using NServiceBus;

namespace Server
{
    public class RequestDataMessageHandler : IHandleMessages<RequestDataMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(RequestDataMessage message)
        {
            Console.WriteLine("Received request {0}.", message.DataId);
            Console.WriteLine("String received: {0}.", message.String);
            Console.WriteLine("Secret Question received: {0}.", message.SecretQuestion.Value);
            Console.WriteLine("Header 'Test' = {0}.", Bus.CurrentMessageContext.Headers["Test"]);

            Bus.OutgoingHeaders["Test"] = Bus.CurrentMessageContext.Headers["Test"];
            Bus.OutgoingHeaders["1"] = "1";
            Bus.OutgoingHeaders["2"] = "2";

            Bus.Reply<DataResponseMessage>(m => 
            { 
                m.DataId = message.DataId;
                m.String = message.String;
                m.SecretAnswer = message.SecretQuestion;
            });
        }
    }
}
