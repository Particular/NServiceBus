namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;

    class MessageProcessingContext : BusContext, IMessageProcessingContext
    {
        public MessageProcessingContext(IncomingContext context, BusOperations busOperations)
            : base(context, busOperations)
        {
            this.context = context;
            incomingMessage = context.Get<IncomingMessage>();
        }

        public string MessageId => incomingMessage.MessageId;

        public string ReplyToAddress => incomingMessage.GetReplyToAddress();

        public IReadOnlyDictionary<string, string> MessageHeaders => incomingMessage.Headers;

        public Task ReplyAsync(object message, ReplyOptions options)
        {
            return BusOperations.ReplyAsync(message, options, context);
        }

        public Task ReplyAsync<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            return BusOperations.ReplyAsync(messageConstructor, options, context);
        }

        public Task ForwardCurrentMessageToAsync(string destination)
        {
            return BusOperations.ForwardCurrentMessageToAsync(destination, context);
        }

        IncomingContext context;

        IncomingMessage incomingMessage;
    }
}