using Messages;
using NServiceBus;
using System;

namespace Subscriber2
{
    public class EventMessageHandler : IMessageHandler<IEvent>
    {
        public void Handle(IEvent message)
        {
            Console.WriteLine("Subscriber 2 received IEvent with Id {0}.", message.EventId);
            Console.WriteLine("Message time: {0}.", message.Time);
            Console.WriteLine("Message duration: {0}.", message.Duration);
        }
    }
}
