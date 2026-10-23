#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using Pipeline;

/// <summary>
/// The root context of a send, publish or reply pipeline. It owns the header dictionary rented for the message, which
/// every context further down the pipeline shares, so the headers are only returned to the pool once the whole pipeline
/// has completed and behaviors can still read them after <c>next()</c>. When the headers are handed to a pending
/// transport operation instead, that operation owns them and they are returned after it has been dispatched.
/// </summary>
abstract class OutgoingRootContext : OutgoingContext
{
    protected OutgoingRootContext(string messageId, Dictionary<string, string> headers, IBehaviorContext parentContext)
        : base(messageId, headers, parentContext) =>
        Set<OutgoingRootContext>(this);

    public bool HeadersHandedToPendingOperations { get; set; }

    /// <summary>
    /// Finds the closest send, publish or reply pipeline whose root context rented <paramref name="headers"/>.
    /// </summary>
    public static OutgoingRootContext? FindOwner(IBehaviorContext context, Dictionary<string, string> headers) =>
        context.Extensions.TryGet(out OutgoingRootContext? owner) && ReferenceEquals(owner.Headers, headers) ? owner : null;
}
