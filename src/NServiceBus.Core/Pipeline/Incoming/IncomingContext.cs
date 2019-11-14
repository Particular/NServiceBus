namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pipeline;

    abstract class IncomingContext : BehaviorContext, IIncomingContext
    {
        protected IncomingContext(string messageId, string replyToAddress, IReadOnlyDictionary<string, string> headers, IBehaviorContext parentContext)
            : base(parentContext)
        {
            MessageId = messageId;
            ReplyToAddress = replyToAddress;
            MessageHeaders = headers;
        }

        MessageOperations messageOperations => Extensions.Get<MessageOperations>();

        public string MessageId { get; }

        public string ReplyToAddress { get; }

        public IReadOnlyDictionary<string, string> MessageHeaders { get; }

        public Task Send(object message, SendOptions options)
        {
            return messageOperations.Send(this, message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return messageOperations.Send(this, messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return messageOperations.Publish(this, message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return messageOperations.Publish(this, messageConstructor, publishOptions);
        }

        public Task Reply(object message, ReplyOptions options)
        {
            return messageOperations.Reply(this, message, options);
        }

        public Task Reply<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            return messageOperations.Reply(this, messageConstructor, options);
        }

        public Task ForwardCurrentMessageTo(string destination)
        {
            return IncomingMessageOperations.ForwardCurrentMessageTo(this, destination);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return messageOperations.Subscribe(this, eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return messageOperations.Unsubscribe(this, eventType, options);
        }
    }
}