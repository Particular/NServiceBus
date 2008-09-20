using System;
using Messages;
using NServiceBus;

namespace Server
{
    public class RequestDataMessageHandler : BaseMessageHandler<RequestDataMessage>, IMessageHandler<IRequestDataMessage>
    {
        public override void Handle(RequestDataMessage message)
        {
            Do(message.DataId);
        }

        public void Handle(IRequestDataMessage message)
        {
            Do(message.DataId);
        }

        public void Do(Guid dataId)
        {
            Console.WriteLine("Received request {0}.", dataId);
            Console.Out.WriteLine("Header 'Test' = {0}.", Bus.IncomingHeaders["Test"]);

            DataResponseMessage response = new DataResponseMessage();
            response.DataId = dataId;
            response.Description = (dataId.ToString("N"));

            Bus.OutgoingHeaders["Test"] = "server1111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111";
            Bus.OutgoingHeaders["1"] = "1";
            Bus.OutgoingHeaders["2"] = "2";

            this.Bus.Reply(response);
        }
    }
}
