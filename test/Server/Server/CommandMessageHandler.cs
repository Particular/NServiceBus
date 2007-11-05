using System;
using System.Collections.Generic;
using System.Text;
using Messages;
using System.Collections;
using NServiceBus;

namespace Server
{
    public class CommandMessageHandler : BaseMessageHandler<Command>
    {
        public override void Handle(Command message)
        {
            this.Bus.Publish(new Event());
        }
    }
}
