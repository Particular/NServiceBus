using Messages;
using NServiceBus;
using System;

namespace Subscriber1
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
        }

        #endregion

        public void Do(Guid id)
        {
            Console.WriteLine("Subscriber 1 received event with Id {0}.", id);
        }
    }
}
