namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MessageInterfaces;
    using Pipeline;

    static class MessageOperations
    {
        public static Task Publish<T>(IBehaviorContext context, Action<T> messageConstructor, PublishOptions options)
        {
            var mapper = context.Extensions.Get<IMessageMapper>();

            return Publish(context, typeof(T), mapper.CreateInstance(messageConstructor), options);
        }

        public static Task Publish(IBehaviorContext context, object message, PublishOptions options)
        {
            var mapper = context.Extensions.Get<IMessageMapper>();
            var messageType = mapper.GetMappedTypeFor(message.GetType());

            return Publish(context, messageType, message, options);
        }

        static Task Publish(IBehaviorContext context, Type messageType, object message, PublishOptions options)
        {
            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<IOutgoingPublishContext>();

            var messageId = options.UserDefinedMessageId ?? CombGuid.Generate().ToString();
            var headers = new Dictionary<string, string>(options.OutgoingHeaders)
            {
                [Headers.MessageId] = messageId
            };

            var publishContext = new OutgoingPublishContext(
                new OutgoingLogicalMessage(messageType, message),
                messageId,
                headers,
                options.Context,
                context);

            return pipeline.Invoke(publishContext);
        }

        public static Task Subscribe(IBehaviorContext context, Type eventType, SubscribeOptions options)
        {
            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<ISubscribeContext>();

            var subscribeContext = new SubscribeContext(
                context,
                eventType,
                options.Context);

            return pipeline.Invoke(subscribeContext);
        }

        public static Task Unsubscribe(IBehaviorContext context, Type eventType, UnsubscribeOptions options)
        {
            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<IUnsubscribeContext>();

            var subscribeContext = new UnsubscribeContext(
                context,
                eventType,
                options.Context);

            return pipeline.Invoke(subscribeContext);
        }

        public static Task Send<T>(IBehaviorContext context, Action<T> messageConstructor, SendOptions options)
        {
            var mapper = context.Extensions.Get<IMessageMapper>();

            return SendMessage(context, typeof(T), mapper.CreateInstance(messageConstructor), options);
        }

        public static Task Send(IBehaviorContext context, object message, SendOptions options)
        {
            var mapper = context.Extensions.Get<IMessageMapper>();
            var messageType = mapper.GetMappedTypeFor(message.GetType());

            return SendMessage(context, messageType, message, options);
        }

        static Task SendMessage(this IBehaviorContext context, Type messageType, object message, SendOptions options)
        {
            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<IOutgoingSendContext>();

            var messageId = options.UserDefinedMessageId ?? CombGuid.Generate().ToString();
            var headers = new Dictionary<string, string>(options.OutgoingHeaders)
            {
                [Headers.MessageId] = messageId
            };

            var outgoingContext = new OutgoingSendContext(
                new OutgoingLogicalMessage(messageType, message),
                messageId,
                headers,
                options.Context,
                context);

            return pipeline.Invoke(outgoingContext);
        }

        public static Task Reply(IBehaviorContext context, object message, ReplyOptions options)
        {
            var mapper = context.Extensions.Get<IMessageMapper>();
            var messageType = mapper.GetMappedTypeFor(message.GetType());

            return ReplyMessage(context, messageType, message, options);
        }

        public static Task Reply<T>(IBehaviorContext context, Action<T> messageConstructor, ReplyOptions options)
        {
            var mapper = context.Extensions.Get<IMessageMapper>();

            return ReplyMessage(context, typeof(T), mapper.CreateInstance(messageConstructor), options);
        }

        static Task ReplyMessage(this IBehaviorContext context, Type messageType, object message, ReplyOptions options)
        {
            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<IOutgoingReplyContext>();

            var messageId = options.UserDefinedMessageId ?? CombGuid.Generate().ToString();
            var headers = new Dictionary<string, string>(options.OutgoingHeaders)
            {
                [Headers.MessageId] = messageId
            };

            var outgoingContext = new OutgoingReplyContext(
                new OutgoingLogicalMessage(messageType, message),
                messageId,
                headers,
                options.Context,
                context);

            return pipeline.Invoke(outgoingContext);

        }
    }
}