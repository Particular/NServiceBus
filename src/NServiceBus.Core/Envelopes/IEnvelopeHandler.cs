namespace NServiceBus;

using System;
using System.Collections.Generic;
using Extensibility;
using Transport;

/// <summary>
/// Handler for unwrapping incoming message envelope formats.
/// </summary>
public interface IEnvelopeHandler
{
    /// <summary>
    /// Create the incoming message context from the transport prior to passing the incoming message to the pipeline.
    /// </summary>
    /// <param name="nativeMessageId">The native message id provided by the transport. This is included for reference purposes, and should be considered readonly.</param>
    /// <param name="incomingHeaders">Headers provided by the transport.</param>
    /// <param name="extensions">ContextBag of extension values provided by the transport.</param>
    /// <param name="incomingBody">The raw body provided by the transport.</param>
    /// <returns></returns>
    (Dictionary<string, string> headers, ReadOnlyMemory<byte> body) CreateIncomingMessage(string nativeMessageId, IDictionary<string, string> incomingHeaders, ContextBag extensions, ReadOnlyMemory<byte> incomingBody);

    /// <summary>
    /// Validates the given <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="messageContext">The context of the incoming message.</param>
    /// <returns>True if the message is valid; otherwise, false.</returns>
    bool IsValidMessage(MessageContext messageContext);
}