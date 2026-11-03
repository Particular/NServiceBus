namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Extensibility;
using MessageInterfaces;
using Microsoft.Extensions.Logging;
using Pipeline;
using Transport;

class MessageOperations(
    IMessageMapper messageMapper,
    IPipeline<IOutgoingPublishContext> publishPipeline,
    IPipeline<IOutgoingSendContext> sendPipeline,
    IPipeline<IOutgoingReplyContext> replyPipeline,
    IPipeline<ISubscribeContext> subscribePipeline,
    IPipeline<IUnsubscribeContext> unsubscribePipeline,
    IActivityFactory activityFactory,
    ILogger<MessageOperations> logger)
{
    protected readonly IPipeline<IOutgoingPublishContext> publishPipeline = publishPipeline;
    protected readonly IPipeline<IOutgoingSendContext> sendPipeline = sendPipeline;
    protected readonly IPipeline<IOutgoingReplyContext> replyPipeline = replyPipeline;
    protected readonly IPipeline<ISubscribeContext> subscribePipeline = subscribePipeline;
    protected readonly IPipeline<IUnsubscribeContext> unsubscribePipeline = unsubscribePipeline;

    public Task Publish<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(IBehaviorContext context, T message, PublishOptions options)
    {
        return PublishMessage(context, typeof(T), message!, options);
    }

    public Task Publish(IBehaviorContext context, object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, PublishOptions options)
    {
        MessageTypeValidator.Validate(message, messageType);
        return PublishMessage(context, messageType, message, options);
    }

    public Task Publish<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(IBehaviorContext context, Action<T> messageConstructor, PublishOptions options)
    {
        return PublishMessage(context, typeof(T), messageMapper.CreateInstance(messageConstructor), options);
    }

    [RequiresUnreferencedCode(RuntimeTypeRoutingTrimmingMessage)]
    public Task Publish(IBehaviorContext context, object message, PublishOptions options)
    {
        var messageType = messageMapper.GetMappedTypeFor(message.GetType());

        return PublishMessage(context, messageType, message, options);
    }

    async Task PublishMessage(IBehaviorContext context, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, object message, PublishOptions options)
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

        var operationName = V11BehaviorSwitch.UseV11Behavior
            ? $"{ActivityDisplayNames.PublishOperation} {messageType.Name}"
            : ActivityDisplayNames.PublishEvent;

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.OutgoingEventActivityName, operationName, publishContext);

#pragma warning disable PS0019 // When catching System.Exception, cancellation needs to be properly accounted for - recording and rethrowing
        try
        {
            await publishPipeline.Invoke(publishContext)
                .ConfigureAwait(false);
            LegacyStatus.SetOk(activity); // removed in v11, see obsoletes-v10.cs
        }
        catch (Exception ex)
        {
            HandleException(context, activity, ex, operationName);
            throw;
        }
    }

    public Task Subscribe(IBehaviorContext context, Type eventType, SubscribeOptions options)
    {
        return Subscribe(context, [eventType], options);
    }

    public async Task Subscribe(IBehaviorContext context, Type[] eventTypes, SubscribeOptions options)
    {
        var subscribeContext = new SubscribeContext(
            context,
            eventTypes,
            options.Context);

        MergeDispatchProperties(subscribeContext, options.DispatchProperties);

        var operationName = V11BehaviorSwitch.UseV11Behavior
            ? $"{ActivityDisplayNames.SubscribeEvent} {string.Join(' ', eventTypes.Select(x => x.Name))}"
            : ActivityDisplayNames.SubscribeEvent;

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.SubscribeActivityName, operationName, context);

        try
        {
            await subscribePipeline.Invoke(subscribeContext)
                .ConfigureAwait(false);
            LegacyStatus.SetOk(activity); // removed in v11, see obsoletes-v10.cs
        }
        catch (Exception ex)
        {
            HandleException(context, activity, ex, operationName);
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

        var operationName = V11BehaviorSwitch.UseV11Behavior
            ? $"{ActivityDisplayNames.UnsubscribeEvent} {eventType.Name}"
            : ActivityDisplayNames.UnsubscribeEvent;

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.UnsubscribeActivityName, operationName, context);

#pragma warning disable PS0019 // When catching System.Exception, cancellation needs to be properly accounted for - recording and rethrowing
        try
        {
            await unsubscribePipeline.Invoke(unsubscribeContext)
                .ConfigureAwait(false);
            LegacyStatus.SetOk(activity); // removed in v11, see obsoletes-v10.cs
        }
        catch (Exception ex)
        {
            HandleException(context, activity, ex, operationName);
            throw;
        }
    }

    public Task Send<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(IBehaviorContext context, T message, SendOptions options)
    {
        return SendMessage(context, typeof(T), message!, options);
    }

    public Task Send(IBehaviorContext context, object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, SendOptions options)
    {
        MessageTypeValidator.Validate(message, messageType);
        return SendMessage(context, messageType, message, options);
    }

    public Task Send<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(IBehaviorContext context, Action<T> messageConstructor, SendOptions options)
    {
        return SendMessage(context, typeof(T), messageMapper.CreateInstance(messageConstructor), options);
    }

    [RequiresUnreferencedCode(RuntimeTypeRoutingTrimmingMessage)]
    public Task Send(IBehaviorContext context, object message, SendOptions options)
    {
        var messageType = messageMapper.GetMappedTypeFor(message.GetType());

        return SendMessage(context, messageType, message, options);
    }

    async Task SendMessage(IBehaviorContext context, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, object message, SendOptions options)
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

        var operationName = V11BehaviorSwitch.UseV11Behavior
            ? $"{ActivityDisplayNames.SendMessage} {messageType.Name}"
            : ActivityDisplayNames.SendMessage;

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.OutgoingMessageActivityName, operationName, outgoingContext);

        try
        {
            await sendPipeline.Invoke(outgoingContext)
                .ConfigureAwait(false);
            LegacyStatus.SetOk(activity); // removed in v11, see obsoletes-v10.cs
        }
        catch (Exception ex)
        {
            HandleException(context, activity, ex, operationName);
            throw;
        }
    }

    public Task Reply<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(IBehaviorContext context, T message, ReplyOptions options)
    {
        return ReplyMessage(context, typeof(T), message!, options);
    }

    public Task Reply(IBehaviorContext context, object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, ReplyOptions options)
    {
        MessageTypeValidator.Validate(message, messageType);
        return ReplyMessage(context, messageType, message, options);
    }

    [RequiresUnreferencedCode(RuntimeTypeRoutingTrimmingMessage)]
    public Task Reply(IBehaviorContext context, object message, ReplyOptions options)
    {
        var messageType = messageMapper.GetMappedTypeFor(message.GetType());

        return ReplyMessage(context, messageType, message, options);
    }

    public Task Reply<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(IBehaviorContext context, Action<T> messageConstructor, ReplyOptions options)
    {
        return ReplyMessage(context, typeof(T), messageMapper.CreateInstance(messageConstructor), options);
    }

    async Task ReplyMessage(IBehaviorContext context, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, object message, ReplyOptions options)
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

        var operationName = V11BehaviorSwitch.UseV11Behavior
            ? $"{ActivityDisplayNames.ReplyMessage} {messageType.Name}"
            : ActivityDisplayNames.ReplyMessage;

        using var activity = activityFactory.StartOutgoingPipelineActivity(ActivityNames.OutgoingMessageActivityName, operationName, context);

        try
        {
            await replyPipeline.Invoke(outgoingContext)
                .ConfigureAwait(false);
            LegacyStatus.SetOk(activity); // removed in v11, see obsoletes-v10.cs
        }
        catch (Exception ex)
        {
            HandleException(context, activity, ex, operationName);
            throw;
        }
    }

    internal const string RuntimeTypeRoutingTrimmingMessage = "When trimming is enabled, routing a message using its runtime type cannot be statically analyzed by the trimmer. Use the generic overload or, when the message type is not known at compile time, the overload accepting an explicit Type.";
    internal const string DefaultInterfaceTrimmingSuppressionJustification = "The default interface implementation preserves compatibility with third-party implementations. Built-in implementations override this method and preserve the declared message type.";

    // The span name is only built when a span is recorded, so the caller builds it for the log entry when there is no span.
    void HandleException(IBehaviorContext context, Activity activity, Exception exception, string displayName)
    {
        activityFactory.RecordError(activity, exception, context.Extensions);

        // Inside message processing recoverability logs the exception with its decision, and a failure of
        // recoverability itself is raised as a critical error. Outside of it, no one else sees the exception.
        if (!IsInsideMessageProcessing(context))
        {
            logger.OperationFailed(exception, displayName);
        }
    }

    // An outgoing context created while a message is processed is not an incoming context, but its context bag
    // chain holds the incoming message that the transport receive context set.
    static bool IsInsideMessageProcessing(IBehaviorContext context) =>
        context is IIncomingContext or ITransportReceiveContext or IRecoverabilityContext
        || context.Extensions.TryGet<IncomingMessage>(out _);

    static void MergeDispatchProperties(ContextBag context, DispatchProperties dispatchProperties)
    {
        // we can't add the constraints directly to the SendOptions ContextBag as the options can be reused
        context.Set(new DispatchProperties(dispatchProperties));
    }
}

static partial class MessageOperationsLoggerMessages
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "An exception occurred while executing '{Operation}'.")]
    public static partial void OperationFailed(this ILogger logger, Exception exception, string operation);
}