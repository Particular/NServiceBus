#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using DelayedDelivery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pipeline;
using Recoverability;
using Routing;
using Transport;

/// <summary>
/// Indicates recoverability is required to delay retry the current message.
/// </summary>
public class DelayedRetry : RecoverabilityAction
{
    /// <summary>
    /// Creates the action with the requested delay.
    /// </summary>
    protected internal DelayedRetry(TimeSpan delay) => Delay = delay;

    /// <summary>
    /// The retry delay.
    /// </summary>
    public TimeSpan Delay { get; }

    /// <summary>
    /// The ErrorHandleResult that should be passed to the transport.
    /// </summary>
    public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.Handled;

    /// <inheritdoc />
    public override IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IRecoverabilityActionContext context)
    {
        var exception = context.Exception;

        var outgoingMessage = new OutgoingMessage(context.MessageId, new Dictionary<string, string>(context.Headers), context.Body);

        var currentDelayedRetriesAttempt = context.DelayedDeliveriesPerformed + 1;

        context.Builder.GetRequiredService<ILogger<DelayedRetry>>().DelayedRetryLogged(exception, context.MessageId, Delay);

        if (context is IRecoverabilityActionContextNotifications notifications)
        {
            notifications.Add(new MessageToBeRetried(
                attempt: currentDelayedRetriesAttempt,
                delay: Delay,
                immediateRetry: false,
                context.NativeMessageId,
                context.MessageId,
                context.Headers,
                context.Body,
                context.ReceiveProperties,
                exception));
        }

        // Metadata is normally only written to headers during move to error, but when we are
        // doing delayed retries, we need to write it to the headers as well.
        if (context.Metadata.TryGetValue(Headers.StartNewTrace, out var startNewTrace))
        {
            outgoingMessage.Headers[Headers.StartNewTrace] = startNewTrace;
        }

        outgoingMessage.SetCurrentDelayedDeliveries(currentDelayedRetriesAttempt);
        outgoingMessage.SetDelayedDeliveryTimestamp(DateTimeOffset.UtcNow);

        var routingContext = context.CreateRoutingContext(outgoingMessage, new UnicastRoutingStrategy(context.ReceiveAddress));
        routingContext.Extensions.Set(new DispatchProperties
        {
            DelayDeliveryWith = new DelayDeliveryWith(Delay)
        });
        return [routingContext];
    }
}

static partial class DelayedRetryLoggerMessages
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Delayed Retry will reschedule message '{MessageId}' after a delay of {Delay} because of an exception:")]
    public static partial void DelayedRetryLogged(this ILogger logger, Exception exception, string messageId, TimeSpan delay);
}