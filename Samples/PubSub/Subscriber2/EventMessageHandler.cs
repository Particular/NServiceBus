using Messages;
using NServiceBus;
using System;

namespace Subscriber2
{
    public class EventMessageHandler : IMessageHandler<EventMessage>, IMessageHandler<IEvent>
    {
        public void Handle(EventMessage message)
        {
            Do(message.EventId);
        }

        #region IMessageHandler<IEvent> Members

        public void Handle(IEvent message)
        {
            Do(message.EventId);
            Console.WriteLine("Message time: {0}.", message.Time);
            Console.WriteLine("Message duration: {0}.", message.Duration);
        }

        #endregion

        public void Do(Guid id)
        {
            Console.WriteLine("Subscriber 2 received event with Id {0}.", id);
        }
    }
}
