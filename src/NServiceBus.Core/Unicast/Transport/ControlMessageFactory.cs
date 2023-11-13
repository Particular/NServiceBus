namespace NServiceBus.Unicast.Transport;

using System;
using NServiceBus.Transport;

/// <summary>
/// Helper for creating control messages.
/// </summary>
public static class ControlMessageFactory
{
    /// <summary>
    /// Creates Transport Message.
    /// </summary>
    /// <returns>Transport Message.</returns>
    public static OutgoingMessage Create(MessageIntent intent)
    {
        var message = new OutgoingMessage(CombGuid.Generate().ToString(), [], Array.Empty<byte>());
        message.Headers[Headers.ControlMessageHeader] = bool.TrueString;
        message.Headers[Headers.MessageIntent] = intent.ToString();
        return message;
    }
}