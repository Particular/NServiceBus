namespace NServiceBus;

using System;
using System.Collections.Generic;
using Extensibility;

/// <summary>
/// Handler for unwrapping incoming message envelope formats.
/// </summary>
public interface IEnvelopeHandler
{
    /// <summary>
    /// Unwraps the incoming message envelope received by the transport prior to passing the incoming message to the pipeline.
    /// </summary>
    /// <param name="nativeMessageId">The native message id provided by the transport. This is included for reference purposes, and should be considered readonly.</param>
    /// <param name="incomingHeaders">Headers provided by the transport.</param>
    /// <param name="extensions">ContextBag of extension values provided by the transport.</param>
    /// <param name="incomingBody">The raw body provided by the transport.</param>
    /// <returns>Dictionary of headers and byte array of message body.</returns>
    (Dictionary<string, string> headers, ReadOnlyMemory<byte> body) UnwrapEnvelope(string nativeMessageId, IDictionary<string, string> incomingHeaders, ContextBag extensions, ReadOnlyMemory<byte> incomingBody);

    /// <summary>
    /// Determines if this envelope handler can unwrap the given message.
    /// </summary>
    /// <param name="nativeMessageId">The native message id provided by the transport. This is included for reference purposes, and should be considered readonly.</param>
    /// <param name="incomingHeaders">Headers provided by the transport.</param>
    /// <param name="extensions">ContextBag of extension values provided by the transport.</param>
    /// <param name="incomingBody">The raw body provided by the transport.</param>
    /// <returns>True if the message envelope can be unwrapped by this handler; otherwise, false.</returns>
    bool CanUnwrapEnvelope(string nativeMessageId, IDictionary<string, string> incomingHeaders, ContextBag extensions, ReadOnlyMemory<byte> incomingBody);
}