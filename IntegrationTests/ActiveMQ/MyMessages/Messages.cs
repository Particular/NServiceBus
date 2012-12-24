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
        Guid CommandId { get; set; }
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
    public class MyRequest1 : IMyCommand
    {
        public Guid CommandId { get; set; }
        public DateTime? Time { get; set; }
        public TimeSpan Duration { get; set; }
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
        public Guid CommandId { get; set; }
        public DateTime? Time { get; set; }
        public TimeSpan Duration { get; set; }
    }
}

namespace MyMessages.Publisher
{
    public enum ResponseCode
    {
        Ok,
        Failed
    };

    public class ResponseToPublisher : IMessage
    {
        public Guid ResponseId { get; set; }
        public DateTime? Time { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class DeferedMessage : IMessage
    {
        public DeferedMessage()
        {
            this.Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }
    }

    public class LocalCommand : IMyCommand
    {
        public Guid CommandId { get; set; }

        public DateTime? Time { get; set; }

        public TimeSpan Duration { get; set; }
    }

    public class StartSagaMessage : IMessage
    {
        public Guid OrderId { get; set; }
    }

    public class StartedSaga : IMessage
    {
        public Guid OrderId { get; set; }
    }
}
