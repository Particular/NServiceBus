using Messages;
using NServiceBus;
using System;

namespace Subscriber2
{
    public class EventMessageHandler : IMessageHandler<EventMessage>
    {
        public void Handle(EventMessage message)
        {
            Console.WriteLine("Subscriber 2 received event with Id {0}.", message.EventId);
        }
    }
}
