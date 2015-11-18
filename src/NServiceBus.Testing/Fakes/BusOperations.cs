namespace NServiceBus.Testing.Fakes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;

    public class BusOperations : IBusContext
    {
        public List<SentMessage> Sent { get; } = new List<SentMessage>();
        public List<PublishedMessage> Published { get; } = new List<PublishedMessage>();
        public List<Subscription> Subscribed { get; } = new List<Subscription>();
        public List<Unsubscription> Unsubscribed { get; } = new List<Unsubscription>();

        public ContextBag Extensions { get; }

        public Task SendAsync(object message, SendOptions options)
        {
            Sent.Add(new SentMessage
            {
                Message = message,
                Options = options
            });

            return Task.CompletedTask;
        }

        public Task SendAsync<T>(Action<T> messageConstructor, SendOptions options)
        {
            throw new NotSupportedException();
        }

        public Task PublishAsync(object message, PublishOptions options)
        {
            Published.Add(new PublishedMessage
            {
                Message = message,
                Options = options
            });

            return Task.CompletedTask;
        }

        public Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            throw new NotSupportedException();
        }

        public Task SubscribeAsync(Type eventType, SubscribeOptions options)
        {
            Subscribed.Add(new Subscription
            {
                EventType = eventType,
                Options = options
            });

            return Task.CompletedTask;
        }

        public Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options)
        {
            Unsubscribed.Add(new Unsubscription
            {
                EventType = eventType,
                Options = options
            });

            return Task.CompletedTask;
        }

        public class SentMessage
        {
            public object Message { get; set; }
            public SendOptions Options { get; set; }
        }

        public class PublishedMessage
        {
            public object Message { get; set; }
            public PublishOptions Options { get; set; }
        }

        public class Subscription
        {
            public Type EventType { get; set; }
            public SubscribeOptions Options { get; set; }
        }

        public class Unsubscription
        {
            public Type EventType { get; set; }
            public UnsubscribeOptions Options { get; set; }
        }
    }
}