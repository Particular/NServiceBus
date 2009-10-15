using NServiceBus;
using System;

namespace MyMessages
{
    [Serializable]
    public class EventMessage : IEvent
    {
        public Guid EventId { get; set; }
        public DateTime? Time { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public interface IEvent : IMessage
    {
        Guid EventId { get; set; }
        DateTime? Time { get; set; }
        TimeSpan Duration { get; set; }
    }
}
