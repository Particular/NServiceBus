namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Extensibility;
using MessageInterfaces;
using Pipeline;
using Transport;

class MessageOperations
{
    readonly IMessageMapper messageMapper;
    protected readonly IPipeline<IOutgoingPublishContext> publishPipeline;
    protected readonly IPipeline<IOutgoingSendContext> sendPipeline;
    protected readonly IPipeline<IOutgoingReplyContext> replyPipeline;
    protected readonly IPipeline<ISubscribeContext> subscribePipeline;
    protected readonly IPipeline<IUnsubscribeContext> unsubscribePipeline;

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
        this.publishPipeline = new TracedPipeline<IOutgoingPublishContext>(publishPipeline, activityFactory, static (factory, context) =>
            factory.StartOutgoingPipelineActivity(
                ActivityNames.OutgoingEventActivityName,
                factory.Options.UseMessageDestinationInSpanNames
                    ? $"{ActivityDisplayNames.PublishOperation} {context.Message.MessageType.Name}"
                    : ActivityDisplayNames.PublishEvent,
                context));
        this.sendPipeline = new TracedPipeline<IOutgoingSendContext>(sendPipeline, activityFactory, static (factory, context) =>
            factory.StartOutgoingPipelineActivity(ActivityNames.OutgoingMessageActivityName, ActivityDisplayNames.SendMessage, context));
        this.replyPipeline = new TracedPipeline<IOutgoingReplyContext>(replyPipeline, activityFactory, static (factory, context) =>
            factory.StartOutgoingPipelineActivity(ActivityNames.OutgoingMessageActivityName, ActivityDisplayNames.ReplyMessage, context));
        this.subscribePipeline = new TracedPipeline<ISubscribeContext>(subscribePipeline, activityFactory, static (factory, context) =>
            factory.StartOutgoingPipelineActivity(ActivityNames.SubscribeActivityName, ActivityDisplayNames.SubscribeEvent, context));
        this.unsubscribePipeline = new TracedPipeline<IUnsubscribeContext>(unsubscribePipeline, activityFactory, static (factory, context) =>
            factory.StartOutgoingPipelineActivity(ActivityNames.UnsubscribeActivityName, ActivityDisplayNames.UnsubscribeEvent, context));
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

        return publishPipeline.Invoke(publishContext);
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

        return subscribePipeline.Invoke(subscribeContext);
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

        MergeDispatchProperties(outgoingContext, options.DispatchProperties);

        return replyPipeline.Invoke(outgoingContext);
    }

    static void MergeDispatchProperties(ContextBag context, DispatchProperties dispatchProperties)
    {
        // we can't add the constraints directly to the SendOptions ContextBag as the options can be reused
        context.Set(new DispatchProperties(dispatchProperties));
    }
}
