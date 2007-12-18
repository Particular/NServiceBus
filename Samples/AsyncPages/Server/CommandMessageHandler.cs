using Messages;
using NServiceBus;
using System.Threading;
using System;

namespace Server
{
    public class CommandMessageHandler : BaseMessageHandler<Command>
    {
        public override void Handle(Command message)
        {
            Thread.Sleep(TimeSpan.FromSeconds(4));

            if (message.Id % 2 == 0)
                this.Bus.Return((int)ErrorCodes.Fail);
            else 
                this.Bus.Return((int)ErrorCodes.None);
        }
    }
}
