using System;
using NServiceBus;

namespace MyMessages
{
    [Serializable]
    public class EventMessage : IMyEvent
    {
        public Guid EventId { get; set; }
        public DateTime? Time { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public interface IMyEvent : IEvent
    {
        Guid EventId { get; set; }
        DateTime? Time { get; set; }
        TimeSpan Duration { get; set; }
    }

    public interface IMyCommand : ICommand
    {
        Guid EventId { get; set; }
        DateTime? Time { get; set; }
        TimeSpan Duration { get; set; }
    }
}

namespace MyMessages.Other
{
    [Serializable]
    public class AnotherEventMessage : IMyEvent
    {
        public Guid EventId { get; set; }
        public DateTime? Time { get; set; }
        public TimeSpan Duration { get; set; }
    }
}

namespace MyMessages.Subscriber1
{
    public interface IMyRequest1 : IMyCommand
    {
    }
}

namespace MyMessages.Subscriber2
{
    public interface IMyRequest2 : IMyCommand
    {
    }
}

namespace MyMessages.SubscriberNMS
{
    public class MyRequestNMS : IMyCommand
    {
        public Guid EventId { get; set; }
        public DateTime? Time { get; set; }
        public TimeSpan Duration { get; set; }
    }
}