namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Extensibility;
using MessageInterfaces;
using Pipeline;
using Transport;

class MessageOperations(
    IMessageMapper messageMapper,
    IPipeline<IOutgoingPublishContext> publishPipeline,
    IPipeline<IOutgoingSendContext> sendPipeline,
    IPipeline<IOutgoingReplyContext> replyPipeline,
    IPipeline<ISubscribeContext> subscribePipeline,
    IPipeline<IUnsubscribeContext> unsubscribePipeline,
    IActivityFactory activityFactory)
{
    protected readonly IPipeline<IOutgoingPublishContext> publishPipeline = publishPipeline;
    protected readonly IPipeline<IOutgoingSendContext> sendPipeline = sendPipeline;
    protected readonly IPipeline<IOutgoingReplyContext> replyPipeline = replyPipeline;
    protected readonly IPipeline<ISubscribeContext> subscribePipeline = subscribePipeline;
    protected readonly IPipeline<IUnsubscribeContext> unsubscribePipeline = unsubscribePipeline;
    readonly bool UseMessageTypeNamesInSpanNames = activityFactory.Options.UseMessageTypeNamesInSpanNames;


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

        var displayName = UseMessageTypeNamesInSpanNames
            ? $"{ActivityDisplayNames.PublishOperation} {messageType.Name}"
            : ActivityDisplayNames.PublishEvent;

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.OutgoingEventActivityName, displayName, publishContext);

#pragma warning disable PS0019 // When catching System.Exception, cancellation needs to be properly accounted for - recording and rethrowing
        try
        {
            await publishPipeline.Invoke(publishContext)
                .ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activityFactory.RecordError(activity, ex, context.Extensions);
            throw;
        }
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

        var displayName = UseMessageTypeNamesInSpanNames
            ? $"{ActivityDisplayNames.SubscribeEvent} {string.Join(' ', eventTypes.Select(x => x.Name))}"
            : ActivityDisplayNames.SubscribeEvent;

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.SubscribeActivityName, displayName, context);

        try
        {
            await subscribePipeline.Invoke(subscribeContext)
                .ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activityFactory.RecordError(activity, ex, context.Extensions);
            throw;
        }
    }

    public async Task Unsubscribe(IBehaviorContext context, Type eventType, UnsubscribeOptions options)
    {
        var unsubscribeContext = new UnsubscribeContext(
            context,
            eventType,
            options.Context
            );

        MergeDispatchProperties(unsubscribeContext, options.DispatchProperties);

        var displayName = UseMessageTypeNamesInSpanNames
            ? $"{ActivityDisplayNames.UnsubscribeEvent} {eventType.Name}"
            : ActivityDisplayNames.UnsubscribeEvent;

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.UnsubscribeActivityName, displayName, context);

#pragma warning disable PS0019 // When catching System.Exception, cancellation needs to be properly accounted for - recording and rethrowing
        try
        {
            await unsubscribePipeline.Invoke(unsubscribeContext)
                .ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activityFactory.RecordError(activity, ex, context.Extensions);
            throw;
        }
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

        var displayName = UseMessageTypeNamesInSpanNames
            ? $"{ActivityDisplayNames.SendMessage} {messageType.Name}"
            : ActivityDisplayNames.SendMessage;

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.OutgoingMessageActivityName, displayName, outgoingContext);

        try
        {
            await sendPipeline.Invoke(outgoingContext)
                .ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activityFactory.RecordError(activity, ex, context.Extensions);
            throw;
        }
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

        var displayName = UseMessageTypeNamesInSpanNames
            ? $"{ActivityDisplayNames.ReplyMessage} {messageType.Name}"
            : ActivityDisplayNames.ReplyMessage;

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.OutgoingMessageActivityName, displayName, context);

        try
        {
            await replyPipeline.Invoke(outgoingContext)
                .ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activityFactory.RecordError(activity, ex, context.Extensions);
            throw;
        }
    }

    static void MergeDispatchProperties(ContextBag context, DispatchProperties dispatchProperties)
    {
        // we can't add the constraints directly to the SendOptions ContextBag as the options can be reused
        context.Set(new DispatchProperties(dispatchProperties));
    }
}