namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Unicast;
    using PublishOptions = NServiceBus.PublishOptions;
    using ReplyOptions = NServiceBus.ReplyOptions;
    using SendOptions = NServiceBus.SendOptions;

    abstract class IncomingContextImpl : BehaviorContextImpl, IncomingContext
    {
        protected IncomingContextImpl(string messageId, string replyToAddress, IReadOnlyDictionary<string, string> headers, BehaviorContext parentContext)
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
            return BusOperationsBehaviorContext.Send(this, message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperationsBehaviorContext.Send(this, messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return BusOperationsBehaviorContext.Publish(this, message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperationsBehaviorContext.Publish(this, messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return BusOperationsBehaviorContext.Subscribe(this, eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return BusOperationsBehaviorContext.Unsubscribe(this, eventType, options);
        }

        public Task Reply(object message, ReplyOptions options)
        {
            return BusOperationsBehaviorContext.Reply(this, message, options);
        }

        public Task Reply<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            return BusOperationsBehaviorContext.Reply(this, messageConstructor, options);
        }

        public Task ForwardCurrentMessageTo(string destination)
        {
            return BusOperationsIncomingContext.ForwardCurrentMessageTo(this, destination);
        }
    }
}