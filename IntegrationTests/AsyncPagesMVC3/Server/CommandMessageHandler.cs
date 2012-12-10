using System;
using System.Threading;
using Messages;
using NServiceBus;

namespace Server
{
    public class CommandMessageHandler : IHandleMessages<Command>
    {
        public IBus Bus { get; set; }

        public void Handle(Command message)
        {
            Console.WriteLine("======================================================================");

            Thread.Sleep(TimeSpan.FromSeconds(1));

            if (message.Id % 2 == 0)
                Bus.Return(ErrorCodes.Fail);
            else
                Bus.Return(ErrorCodes.None);
        }
    }
}
