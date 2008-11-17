using NServiceBus;
using System;

namespace Messages
{
    [Serializable]
    public class EventMessage : IEvent
    {
        private Guid eventId;
        private DateTime time;
        private TimeSpan duration;

        public Guid EventId
        {
            get { return eventId; }
            set { eventId = value; }
        }

        public DateTime Time
        {
            get { return time; }
            set { time = value; }
        }

        public TimeSpan Duration
        {
            get { return duration; }
            set { duration = value; }
        }
    }

    public interface IEvent : IMessage
    {
        Guid EventId { get; set; }
        DateTime Time { get; set; }
        TimeSpan Duration { get; set; }
    }
}
