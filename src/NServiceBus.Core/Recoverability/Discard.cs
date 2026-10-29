#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pipeline;
using Transport;

/// <summary>
/// Indicates recoverability is required to discard/ignore the current message.
/// </summary>
public class Discard : RecoverabilityAction
{
    /// <summary>
    /// Creates the action with the stated reason.
    /// </summary>
    public Discard(string reason) => Reason = reason;

    /// <summary>
    /// The reason why a message was discarded.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// How to handle the message from a transport perspective.
    /// </summary>
    public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.Handled;

    /// <inheritdoc />
    public override IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IRecoverabilityActionContext context)
    {
        context.Builder.GetRequiredService<ILogger<Discard>>().DiscardLogged(context.Exception, context.MessageId, Reason);
        return [];
    }
}

static partial class DiscardLoggerMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Discarding message with id '{MessageId}'. Reason: {Reason}")]
    public static partial void DiscardLogged(this ILogger logger, Exception exception, string messageId, string reason);
}