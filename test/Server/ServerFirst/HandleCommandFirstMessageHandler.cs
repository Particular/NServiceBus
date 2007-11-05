using System;
using System.Collections.Generic;
using System.Text;
using Messages;
using NServiceBus;

namespace ServerFirst
{
    public class HandleCommandFirstMessageHandler : BaseMessageHandler<Command>
    {
        public override void Handle(Command message)
        {
            if (message.i > 25)
            {
                this.Bus.DoNotContinueDispatchingCurrentMessageToHandlers();
                Console.WriteLine("Refused to handle message with value > 25.");
            }
        }
    }
}
