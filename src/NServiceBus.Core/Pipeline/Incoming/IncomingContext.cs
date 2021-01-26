namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pipeline;

    abstract class IncomingContext : BehaviorContext, IIncomingContext
    {
        protected IncomingContext(string messageId, string replyToAddress, IReadOnlyDictionary<string, string> headers, IBehaviorContext parentContext)
            : base(parentContext, parentContext?.CancellationToken ?? default)
        {
            MessageId = messageId;
            ReplyToAddress = replyToAddress;
            MessageHeaders = headers;
        }

        MessageOperations MessageOperations => Extensions.Get<MessageOperations>();

        public string MessageId { get; }

        public string ReplyToAddress { get; }

        public IReadOnlyDictionary<string, string> MessageHeaders { get; }

        public Task Send(object message, SendOptions options)
        {
            return MessageOperations.Send(this, message, options, CancellationToken);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return MessageOperations.Send(this, messageConstructor, options, CancellationToken);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return MessageOperations.Publish(this, message, options, CancellationToken);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return MessageOperations.Publish(this, messageConstructor, publishOptions, CancellationToken);
        }

        public Task Reply(object message, ReplyOptions options)
        {
            return MessageOperations.Reply(this, message, options, CancellationToken);
        }

        public Task Reply<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            return MessageOperations.Reply(this, messageConstructor, options, CancellationToken);
        }

        public Task ForwardCurrentMessageTo(string destination)
        {
            return IncomingMessageOperations.ForwardCurrentMessageTo(this, destination);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return MessageOperations.Subscribe(this, eventType, options, CancellationToken);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return MessageOperations.Unsubscribe(this, eventType, options, CancellationToken);
        }
    }
}