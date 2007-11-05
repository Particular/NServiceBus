using System;
using System.Collections.Generic;
using System.Text;
using KorenTec.Messaging;
using KorenTec.Messages;
using Messages;

namespace Server
{
    public class AddIntFirstMessageHandler : BaseMessageHandler<AddMessage<IntInfo>>
    {
        public override void Handle(AddMessage<IntInfo> message)
        {
            message.Info.k = message.Info.k * 2;
        }
    }
}
