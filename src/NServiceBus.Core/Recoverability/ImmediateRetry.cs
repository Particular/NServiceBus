#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pipeline;
using Transport;

/// <summary>
/// Indicates recoverability is required to immediately retry the current message.
/// </summary>
public class ImmediateRetry : RecoverabilityAction
{
    /// <summary>
    /// Creates an immediate retry action.
    /// </summary>
    protected internal ImmediateRetry()
    {
    }

    /// <summary>
    /// The ErrorHandleResult that should be passed to the transport.
    /// </summary>
    public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.RetryRequired;

    /// <inheritdoc />
    public override IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IRecoverabilityActionContext context)
    {
        var exception = context.Exception;

        context.Builder.GetService<ILogger<ImmediateRetry>>()?.ImmediateRetryLogged(exception, context.MessageId);

        if (context is IRecoverabilityActionContextNotifications notifications)
        {
            notifications.Add(new MessageToBeRetried(
                attempt: context.ImmediateProcessingFailures - 1,
                delay: TimeSpan.Zero,
                immediateRetry: true,
                context.NativeMessageId,
                context.MessageId,
                context.Headers,
                context.Body,
                context.ReceiveProperties,
                exception));
        }
        return [];
    }
}

static partial class ImmediateRetryLoggerMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Immediate Retry is going to retry message '{MessageId}' because of an exception:")]
    public static partial void ImmediateRetryLogged(this ILogger logger, Exception exception, string messageId);
}