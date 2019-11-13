namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using MessageInterfaces;
    using Pipeline;

    class MessageOperations
    {
        IMessageMapper messageMapper;
        readonly IPipeline<IOutgoingPublishContext> publishPipeline;
        readonly IPipeline<IOutgoingSendContext> sendPipeline;
        readonly IPipeline<IOutgoingReplyContext> replyPipeline;
        readonly IPipeline<ISubscribeContext> subscribePipeline;
        readonly IPipeline<IUnsubscribeContext> unsubscribePipeline;

        public MessageOperations(
            IMessageMapper messageMapper, 
            IPipeline<IOutgoingPublishContext> publishPipeline, 
            IPipeline<IOutgoingSendContext> sendPipeline, 
            IPipeline<IOutgoingReplyContext> replyPipeline, 
            IPipeline<ISubscribeContext> subscribePipeline, 
            IPipeline<IUnsubscribeContext> unsubscribePipeline)
        {
            this.messageMapper = messageMapper;
            this.publishPipeline = publishPipeline;
            this.sendPipeline = sendPipeline;
            this.replyPipeline = replyPipeline;
            this.subscribePipeline = subscribePipeline;
            this.unsubscribePipeline = unsubscribePipeline;
        }

        public Task Publish<T>(IBehaviorContext context, Action<T> messageConstructor, PublishOptions options)
        {
            return Publish(context, typeof(T), messageMapper.CreateInstance(messageConstructor), options);
        }

        public Task Publish(IBehaviorContext context, object message, PublishOptions options)
        {
            var messageType = messageMapper.GetMappedTypeFor(message.GetType());

            return Publish(context, messageType, message, options);
        }

        Task Publish(IBehaviorContext context, Type messageType, object message, PublishOptions options)
        {
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

            return publishPipeline.Invoke(publishContext);
        }

        public Task Subscribe(IBehaviorContext context, Type eventType, SubscribeOptions options)
        {
            var subscribeContext = new SubscribeContext(
                context,
                eventType,
                options.Context);

            return subscribePipeline.Invoke(subscribeContext);
        }

        public Task Unsubscribe(IBehaviorContext context, Type eventType, UnsubscribeOptions options)
        {
            var unsubscribeContext = new UnsubscribeContext(
                context,
                eventType,
                options.Context);

            return unsubscribePipeline.Invoke(unsubscribeContext);
        }

        public Task Send<T>(IBehaviorContext context, Action<T> messageConstructor, SendOptions options)
        {
            return SendMessage(context, typeof(T), messageMapper.CreateInstance(messageConstructor), options);
        }

        public Task Send(IBehaviorContext context, object message, SendOptions options)
        {
            var messageType = messageMapper.GetMappedTypeFor(message.GetType());

            return SendMessage(context, messageType, message, options);
        }

        Task SendMessage(IBehaviorContext context, Type messageType, object message, SendOptions options)
        {
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

            if (options.DelayedDeliveryConstraint != null)
            {
                // we can't add the constraints directly to the SendOptions ContextBag as the options can be reused
                // and the delivery constraints might be removed by the TimeoutManager logic.
                outgoingContext.AddDeliveryConstraint(options.DelayedDeliveryConstraint);
            }

            return sendPipeline.Invoke(outgoingContext);
        }

        public Task Reply(IBehaviorContext context, object message, ReplyOptions options)
        {
            var messageType = messageMapper.GetMappedTypeFor(message.GetType());

            return ReplyMessage(context, messageType, message, options);
        }

        public Task Reply<T>(IBehaviorContext context, Action<T> messageConstructor, ReplyOptions options)
        {
            return ReplyMessage(context, typeof(T), messageMapper.CreateInstance(messageConstructor), options);
        }

        Task ReplyMessage(IBehaviorContext context, Type messageType, object message, ReplyOptions options)
        {
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

            return replyPipeline.Invoke(outgoingContext);
        }
    }
}