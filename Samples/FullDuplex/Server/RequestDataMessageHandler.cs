using System;
using Messages;
using NServiceBus;

namespace Server
{
    public class RequestDataMessageHandler : BaseMessageHandler<RequestDataMessage>
    {
        public override void Handle(RequestDataMessage message)
        {
            DataResponseMessage response = new DataResponseMessage();
            response.DataId = message.DataId;
            response.Description = (message.DataId.ToString("N"));

            this.Bus.Reply(response);
        }
    }
}
