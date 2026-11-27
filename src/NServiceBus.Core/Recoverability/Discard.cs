#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
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
    public override IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IRecoverabilityActionContext context) => [];
}