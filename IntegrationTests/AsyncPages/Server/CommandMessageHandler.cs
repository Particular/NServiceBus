using Messages;
using NServiceBus;
using System.Threading;
using System;

namespace Server
{
    public class CommandMessageHandler : IHandleMessages<Command>
    {
        
        /// <summary>
        /// The handler is using the new IHandleMessages&lt;T&gt; extension method of the Bus().
        /// Hence, there is no need to declare on a IBus interface property to be injected.
        /// </summary>
        /// <param name="message"></param>
        public void Handle(Command message)
        {
            Console.WriteLine("======================================================================");

            Thread.Sleep(TimeSpan.FromSeconds(1));

            if (message.Id % 2 == 0)
                this.Bus().Return(ErrorCodes.Fail);
            else 
                this.Bus().Return(ErrorCodes.None);
        }
    }
}
