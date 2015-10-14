namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Unicast;

    class MessageHandlerContext : IMessageHandlerContext
    {
        ContextualBus bus;

        public MessageHandlerContext(ContextualBus bus)
        {
            this.bus = bus;
        }

        public string MessageId => bus.MessageContext.Id;

        public string ReplyToAddress => bus.MessageContext.ReplyToAddress;

        public IReadOnlyDictionary<string, string> MessageHeaders => bus.MessageContext.Headers;

        public Task SendAsync(object message, SendOptions options)
        {
            return bus.SendAsync(message, options);
        }

        public Task SendAsync<T>(Action<T> messageConstructor, SendOptions options)
        {
            return bus.SendAsync(messageConstructor, options);
        }

        public Task PublishAsync(object message, PublishOptions options)
        {
            return bus.PublishAsync(message, options);
        }

        public Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return bus.PublishAsync(messageConstructor, publishOptions);
        }

        public Task ReplyAsync(object message, ReplyOptions options)
        {
            return bus.ReplyAsync(message, options);
        }

        public Task ReplyAsync<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            return bus.ReplyAsync(messageConstructor, options);
        }

        public Task HandleCurrentMessageLaterAsync()
        {
            return bus.HandleCurrentMessageLaterAsync();
        }

        public Task ForwardCurrentMessageToAsync(string destination)
        {
            return bus.ForwardCurrentMessageToAsync(destination);
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            bus.DoNotContinueDispatchingCurrentMessageToHandlers();
        }
    }
}