#nullable enable

namespace NServiceBus;

using System;
using System.Buffers;
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
    /// <param name="bodyWriter">The body writer to write the unwrapped body into.</param>
    /// <param name="nativeMessageId">The native message id provided by the transport. This is included for reference purposes, and should be considered readonly.</param>
    /// <param name="incomingHeaders">Headers provided by the transport.</param>
    /// <param name="extensions">ContextBag of extension values provided by the transport.</param>
    /// <param name="incomingBody">The raw body provided by the transport.</param>
    /// <returns>Dictionary of headers if the message can be unwrapped. Null or exception otherwise.</returns>
    Dictionary<string, string>? UnwrapEnvelope(IBufferWriter<byte> bodyWriter, string nativeMessageId, IDictionary<string, string> incomingHeaders, ContextBag extensions, ReadOnlySpan<byte> incomingBody);
}