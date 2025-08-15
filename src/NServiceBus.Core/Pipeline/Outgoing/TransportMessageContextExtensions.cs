#nullable enable

namespace NServiceBus.Pipeline;

using System;
using System.Diagnostics.CodeAnalysis;
using Transport;

/// <summary>
/// Context extension to provide access to the incoming physical message.
/// </summary>
public static class TransportMessageContextExtensions
{
    /// <summary>
    /// Returns the incoming physical message if there is one currently processed.
    /// </summary>
    public static bool TryGetIncomingPhysicalMessage(this IOutgoingReplyContext context, [NotNullWhen(true)] out IncomingMessage? message)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Extensions.TryGet(out message);
    }

    /// <summary>
    /// Returns the incoming physical message if there is one currently processed.
    /// </summary>
    public static bool TryGetIncomingPhysicalMessage(this IOutgoingLogicalMessageContext context, [NotNullWhen(true)] out IncomingMessage? message)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Extensions.TryGet(out message);
    }

    /// <summary>
    /// Returns the incoming physical message if there is one currently processed.
    /// </summary>
    public static bool TryGetIncomingPhysicalMessage(this IOutgoingPhysicalMessageContext context, [NotNullWhen(true)] out IncomingMessage? message)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Extensions.TryGet(out message);
    }
}