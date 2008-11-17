using Messages;
using NServiceBus;
using System;

namespace Subscriber1
{
    public class EventMessageHandler : IMessageHandler<EventMessage>
    {
        public void Handle(EventMessage message)
        {
            Console.WriteLine("Subscriber 1 received EventMessage with Id {0}.", message.EventId);
            Console.WriteLine("Message time: {0}.", message.Time);
            Console.WriteLine("Message duration: {0}.", message.Duration);
        }
    }
}
