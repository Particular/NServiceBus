namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Settings;

    static class BusOperationsBehaviorContext
    {
        public static Task Publish<T>(BehaviorContext context, Action<T> messageConstructor, PublishOptions options)
        {
            var mapper = context.Builder.Build<IMessageMapper>();
            return Publish(context, mapper.CreateInstance(messageConstructor), options);
        }

        public static Task Publish(BehaviorContext context, object message, PublishOptions options)
        {
            var settings = context.Builder.Build<ReadOnlySettings>();
            var pipeline = new PipelineBase<OutgoingPublishContext>(
                context.Builder, 
                settings, 
                settings.Get<PipelineConfiguration>().MainPipeline);

            var publishContext = new OutgoingPublishContextImpl(
                new OutgoingLogicalMessage(message),
                options,
                context);

            return pipeline.Invoke(publishContext);
        }

        public static Task Subscribe(BehaviorContext context, Type eventType, SubscribeOptions options)
        {
            var settings = context.Builder.Build<ReadOnlySettings>();
            var pipeline = new PipelineBase<SubscribeContext>(context.Builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var subscribeContext = new SubscribeContextImpl(
                context,
                eventType,
                options);

            return pipeline.Invoke(subscribeContext);
        }

        public static Task Unsubscribe(BehaviorContext context, Type eventType, UnsubscribeOptions options)
        {
            var settings = context.Builder.Build<ReadOnlySettings>();
            var pipeline = new PipelineBase<UnsubscribeContext>(context.Builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var subscribeContext = new UnsubscribeContextImpl(
                context,
                eventType,
                options);

            return pipeline.Invoke(subscribeContext);
        }

        public static Task Send<T>(BehaviorContext context, Action<T> messageConstructor, SendOptions options)
        {
            var mapper = context.Builder.Build<IMessageMapper>();
            return Send(context, mapper.CreateInstance(messageConstructor), options);
        }

        public static Task Send(BehaviorContext context, object message, SendOptions options)
        {
            var messageType = message.GetType();

            return context.SendMessage(messageType, message, options);
        }

        static Task SendMessage(this BehaviorContext context, Type messageType, object message, SendOptions options)
        {
            var settings = context.Builder.Build<ReadOnlySettings>();
            var pipeline = new PipelineBase<OutgoingSendContext>(context.Builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var outgoingContext = new OutgoingSendContextImpl(
                new OutgoingLogicalMessage(messageType, message),
                options,
                context);

            return pipeline.Invoke(outgoingContext);
        }

        public static Task Reply(BehaviorContext context, object message, ReplyOptions options)
        {
            var settings = context.Builder.Build<ReadOnlySettings>();
            var pipeline = new PipelineBase<OutgoingReplyContext>(
                context.Builder, 
                settings, 
                settings.Get<PipelineConfiguration>().MainPipeline);

            var outgoingContext = new OutgoingReplyContextImpl(
                new OutgoingLogicalMessage(message),
                options,
                context);

            return pipeline.Invoke(outgoingContext);
        }

        public static Task Reply<T>(BehaviorContext context, Action<T> messageConstructor, ReplyOptions options)
        {
            var mapper = context.Builder.Build<IMessageMapper>();
            return Reply(context, mapper.CreateInstance(messageConstructor), options);
        }
    }
}