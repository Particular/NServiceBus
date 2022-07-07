namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using MessageInterfaces;
    using Pipeline;
    using Transport;

    class MessageOperations
    {
        IMessageMapper messageMapper;
        readonly IPipeline<IOutgoingPublishContext> publishPipeline;
        readonly IPipeline<IOutgoingSendContext> sendPipeline;
        readonly IPipeline<IOutgoingReplyContext> replyPipeline;
        readonly IPipeline<ISubscribeContext> subscribePipeline;
        readonly IPipeline<IUnsubscribeContext> unsubscribePipeline;
        readonly IActivityFactory activityFactory;

        public MessageOperations(
            IMessageMapper messageMapper,
            IPipeline<IOutgoingPublishContext> publishPipeline,
            IPipeline<IOutgoingSendContext> sendPipeline,
            IPipeline<IOutgoingReplyContext> replyPipeline,
            IPipeline<ISubscribeContext> subscribePipeline,
            IPipeline<IUnsubscribeContext> unsubscribePipeline,
            IActivityFactory activityFactory)
        {
            this.messageMapper = messageMapper;
            this.publishPipeline = publishPipeline;
            this.sendPipeline = sendPipeline;
            this.replyPipeline = replyPipeline;
            this.subscribePipeline = subscribePipeline;
            this.unsubscribePipeline = unsubscribePipeline;
            this.activityFactory = activityFactory;
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

            MergeDispatchProperties(publishContext, options.DispatchProperties);

            using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.OutgoingEventActivityName, "publish event", publishContext);
            // TODO: This call should be awaited or the activity will be disposed too early
            return publishPipeline.Invoke(publishContext, activity);
        }

        public Task Subscribe(IBehaviorContext context, Type eventType, SubscribeOptions options)
        {
            return Subscribe(context, new Type[] { eventType }, options);
        }

        public Task Subscribe(IBehaviorContext context, Type[] eventTypes, SubscribeOptions options)
        {
            var subscribeContext = new SubscribeContext(
                context,
                eventTypes,
                options.Context);

            MergeDispatchProperties(subscribeContext, options.DispatchProperties);

            using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.SubscribeActivityName, "subscribe event", context);

            return subscribePipeline.Invoke(subscribeContext, activity);
        }

        public Task Unsubscribe(IBehaviorContext context, Type eventType, UnsubscribeOptions options)
        {
            var unsubscribeContext = new UnsubscribeContext(
                context,
                eventType,
                options.Context);

            MergeDispatchProperties(unsubscribeContext, options.DispatchProperties);

            using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.UnsubscribeActivityName, "unsubscribe event", context);

            return unsubscribePipeline.Invoke(unsubscribeContext, activity);
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

            MergeDispatchProperties(outgoingContext, options.DispatchProperties);

            using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.OutgoingMessageActivityName, "send message", outgoingContext);

            return sendPipeline.Invoke(outgoingContext, activity);
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

            MergeDispatchProperties(outgoingContext, options.DispatchProperties);

            using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.OutgoingMessageActivityName, "reply", outgoingContext);

            return replyPipeline.Invoke(outgoingContext, activity);
        }

        static void MergeDispatchProperties(ContextBag context, DispatchProperties dispatchProperties)
        {
            // we can't add the constraints directly to the SendOptions ContextBag as the options can be reused
            context.Set(new DispatchProperties(dispatchProperties));
        }
    }
}