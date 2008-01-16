using NServiceBus;
using System;

namespace Messages
{
    [Serializable]
    public class EventMessage : IMessage
    {
        public Guid EventId;
    }
}
