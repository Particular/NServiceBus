#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using NServiceBus.Logging;
using NServiceBus.Transport;
using Pipeline;

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

        Logger.Info($"Immediate Retry is going to retry message '{context.MessageId}' because of an exception:", exception);
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

    static readonly ILog Logger = LogManager.GetLogger<ImmediateRetry>();
}