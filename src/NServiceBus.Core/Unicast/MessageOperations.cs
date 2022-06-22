namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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

            MergeDispatchProperties(publishContext, options.DispatchProperties);

            return InvokePipelineWithTracing(ActivityNames.OutgoingEventActivityName, publishContext, publishPipeline);
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

            return InvokePipelineWithTracing(ActivityNames.SubscribeActivityName, subscribeContext, subscribePipeline);
        }

        public Task Unsubscribe(IBehaviorContext context, Type eventType, UnsubscribeOptions options)
        {
            var unsubscribeContext = new UnsubscribeContext(
                context,
                eventType,
                options.Context);

            MergeDispatchProperties(unsubscribeContext, options.DispatchProperties);

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

            MergeDispatchProperties(outgoingContext, options.DispatchProperties);

            return InvokePipelineWithTracing(ActivityNames.OutgoingMessageActivityName, outgoingContext, sendPipeline);
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

            return InvokePipelineWithTracing(ActivityNames.OutgoingMessageActivityName, outgoingContext, replyPipeline);
        }

        static async Task InvokePipelineWithTracing<TContext>(string activityName, TContext outgoingContext, IPipeline<TContext> pipeline)
            where TContext : IBehaviorContext
        {
            using var activity = ActivitySources.Main.CreateActivity(activityName, ActivityKind.Producer);
            activity?.SetIdFormat(ActivityIdFormat.W3C);
            activity?.Start();
            outgoingContext.Extensions.Set(DiagnosticsKeys.OutgoingActivityKey, activity);

            try
            {
                await pipeline.Invoke(outgoingContext).ConfigureAwait(false);
            }
#pragma warning disable PS0019 // When catching System.Exception, cancellation needs to be properly accounted for
            catch (Exception ex)
#pragma warning restore PS0019 // When catching System.Exception, cancellation needs to be properly accounted for
            {
                // TODO: Add an explicit tag for operation canceled
                ActivityDecorator.SetErrorStatus(activity, ex);
                throw;
            }
            // TODO: should we stop the activity only once the message has been handed to the dispatcher?
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        static void MergeDispatchProperties(ContextBag context, DispatchProperties dispatchProperties)
        {
            // we can't add the constraints directly to the SendOptions ContextBag as the options can be reused
            context.Set(new DispatchProperties(dispatchProperties));
        }
    }
}