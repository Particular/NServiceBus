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
    protected readonly IActivityFactory activityFactory;

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

    async Task Publish(IBehaviorContext context, Type messageType, object message, PublishOptions options)
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

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.OutgoingEventActivityName, ActivityDisplayNames.PublishEvent, publishContext);

        await publishPipeline.Invoke(publishContext, activity).ConfigureAwait(false);
    }

    public Task Subscribe(IBehaviorContext context, Type eventType, SubscribeOptions options)
    {
        return Subscribe(context, new Type[] { eventType }, options);
    }

    public async Task Subscribe(IBehaviorContext context, Type[] eventTypes, SubscribeOptions options)
    {
        var subscribeContext = new SubscribeContext(
            context,
            eventTypes,
            options.Context);

        MergeDispatchProperties(subscribeContext, options.DispatchProperties);

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.SubscribeActivityName, ActivityDisplayNames.SubscribeEvent, context);

        await subscribePipeline.Invoke(subscribeContext, activity).ConfigureAwait(false);
    }

    public async Task Unsubscribe(IBehaviorContext context, Type eventType, UnsubscribeOptions options)
    {
        var unsubscribeContext = new UnsubscribeContext(
            context,
            eventType,
            options.Context);

        MergeDispatchProperties(unsubscribeContext, options.DispatchProperties);

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.UnsubscribeActivityName, ActivityDisplayNames.UnsubscribeEvent, context);

        await unsubscribePipeline.Invoke(unsubscribeContext, activity).ConfigureAwait(false);
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

    async Task SendMessage(IBehaviorContext context, Type messageType, object message, SendOptions options)
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

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.OutgoingMessageActivityName, ActivityDisplayNames.SendMessage, outgoingContext);

        await sendPipeline.Invoke(outgoingContext, activity).ConfigureAwait(false);
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

    async Task ReplyMessage(IBehaviorContext context, Type messageType, object message, ReplyOptions options)
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

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.OutgoingMessageActivityName, ActivityDisplayNames.ReplyMessage, outgoingContext);

        await replyPipeline.Invoke(outgoingContext, activity).ConfigureAwait(false);
    }

    static void MergeDispatchProperties(ContextBag context, DispatchProperties dispatchProperties)
    {
        // we can't add the constraints directly to the SendOptions ContextBag as the options can be reused
        context.Set(new DispatchProperties(dispatchProperties));
    }
}