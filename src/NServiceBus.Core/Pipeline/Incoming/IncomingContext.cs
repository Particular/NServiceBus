namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    abstract class IncomingContext : BehaviorContext, IIncomingContext
    {
        protected IncomingContext(string messageId, string replyToAddress, IReadOnlyDictionary<string, string> headers, IBehaviorContext parentContext)
            : base(parentContext)
        {

            MessageId = messageId;
            ReplyToAddress = replyToAddress;
            MessageHeaders = headers;
        }

        public string MessageId { get; }

        public string ReplyToAddress { get; }

        public IReadOnlyDictionary<string, string> MessageHeaders { get; }

        public Task Send(object message, SendOptions options)
        {
            return BusOperations.Send(this, message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperations.Send(this, messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return BusOperations.Publish(this, message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperations.Publish(this, messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return BusOperations.Subscribe(this, eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return BusOperations.Unsubscribe(this, eventType, options);
        }

        public Task Reply(object message, ReplyOptions options)
        {
            return BusOperations.Reply(this, message, options);
        }

        public Task Reply<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            return BusOperations.Reply(this, messageConstructor, options);
        }

        public Task ForwardCurrentMessageTo(string destination)
        {
            return IncomingBusOperations.ForwardCurrentMessageTo(this, destination);
        }
    }
}