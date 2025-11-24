namespace NServiceBus;

using System;
using System.Collections.Generic;
using Extensibility;
using Transport;

public interface IEnvelopeHandler
{
    (IDictionary<string, string> headers, ReadOnlyMemory<byte> body) CreateIncomingMessage(string nativeMessageId, IDictionary<string, string> headers, ContextBag contextBag, ReadOnlyMemory<byte> body);

    /// <summary>
    /// Validates the given <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="messageContext">The context of the incoming message.</param>
    /// <returns>True if the message is valid; otherwise, false.</returns>
    bool IsValidMessage(MessageContext messageContext);
}