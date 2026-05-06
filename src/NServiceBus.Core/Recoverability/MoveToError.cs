#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using Logging;
using Pipeline;
using Recoverability;
using Routing;
using Transport;

/// <summary>
/// Indicates that recoverability is required to move the current message to the error queue.
/// </summary>
public class MoveToError : RecoverabilityAction
{
    /// <summary>
    /// Creates the action with the target error queue.
    /// </summary>
    protected internal MoveToError(string errorQueue) => ErrorQueue = errorQueue;

    /// <summary>
    /// Defines the error queue where the message should be move to.
    /// </summary>
    public string ErrorQueue { get; }

    /// <summary>
    /// The ErrorHandleResult that should be passed to the transport.
    /// </summary>
    public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.Handled;

    /// <inheritdoc />
    public override IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IRecoverabilityActionContext context)
    {
        var metadata = context.Metadata;
        var exception = context.Exception;

        Logger.Error($"Moving message '{context.MessageId}' to the error queue '{ErrorQueue}' because processing failed due to an exception:", exception);

        if (context is IRecoverabilityActionContextNotifications notifications)
        {
            notifications.Add(new MessageFaulted(ErrorQueue, context.NativeMessageId, context.MessageId, context.Headers, context.Body, context.ReceiveProperties, exception));
        }

        var outgoingMessageHeaders = new Dictionary<string, string>(context.Headers);
        _ = outgoingMessageHeaders.Remove(Headers.DelayedRetries);
        _ = outgoingMessageHeaders.Remove(Headers.ImmediateRetries);
        var outgoingMessage = new OutgoingMessage(context.MessageId, outgoingMessageHeaders, context.Body);

        foreach (var faultMetadata in metadata)
        {
            outgoingMessageHeaders[faultMetadata.Key] = faultMetadata.Value;
        }
        return
        [
            context.CreateRoutingContext(outgoingMessage, new UnicastRoutingStrategy(ErrorQueue))
        ];
    }

    static readonly ILog Logger = LogManager.GetLogger<MoveToError>();
}