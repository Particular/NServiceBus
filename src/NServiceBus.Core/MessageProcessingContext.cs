namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;

    class MessageProcessingContext : IMessageProcessingContext
    {
        public MessageProcessingContext(IncomingContext context)
        {
            this.context = context;
            bus = context.Builder.Build<StaticBus>();
            incomingMessage = context.Get<IncomingMessage>();
            Extensions = context;
        }

        public string MessageId => incomingMessage.MessageId;

        public string ReplyToAddress => incomingMessage.GetReplyToAddress();

        public IReadOnlyDictionary<string, string> MessageHeaders => incomingMessage.Headers;

        public ContextBag Extensions { get; }

        public Task SendAsync(object message, SendOptions options)
        {
            return bus.SendAsync(message, options, context);
        }

        public Task SendAsync<T>(Action<T> messageConstructor, SendOptions options)
        {
            return bus.SendAsync(messageConstructor, options, context);
        }

        public Task PublishAsync(object message, PublishOptions options)
        {
            return bus.PublishAsync(message, options, context);
        }

        public Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return bus.PublishAsync(messageConstructor, publishOptions, context);
        }

        public Task ReplyAsync(object message, ReplyOptions options)
        {
            return bus.ReplyAsync(message, options, context);
        }

        public Task ReplyAsync<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            return bus.ReplyAsync(messageConstructor, options, context);
        }

        public Task ForwardCurrentMessageToAsync(string destination)
        {
            return bus.ForwardCurrentMessageToAsync(destination, context);
        }

        protected StaticBus bus;
        IncomingMessage incomingMessage;
        IncomingContext context;
    }
}