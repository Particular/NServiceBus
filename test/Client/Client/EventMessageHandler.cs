using System;
using Messages;
using NServiceBus;

namespace Client
{
    public class EventMessageHandler : BaseMessageHandler<Event>
    {
        public override void Handle(Event message)
        {
            Console.WriteLine("Received from {0}", this.Bus.SourceOfMessageBeingHandled);

            if (message.j % 100 == 0)
                Console.WriteLine("Client Handler: {0}.{1}  {2}", DateTime.Now.Second, DateTime.Now.Millisecond, message.j);
        }
    }
}
