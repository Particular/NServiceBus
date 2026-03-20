namespace NServiceBus.Transport;

using System;
using System.Collections.Generic;

/// <summary>
/// The raw message coming from the transport.
/// </summary>
public class IncomingMessage
{
    /// <summary>
    /// Creates a new message.
    /// </summary>
    /// <param name="nativeMessageId">The native message ID.</param>
    /// <param name="headers">The message headers.</param>
    /// <param name="body">The message body.</param>
    public IncomingMessage(string nativeMessageId, Dictionary<string, string> headers, ReadOnlyMemory<byte> body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeMessageId);
        ArgumentNullException.ThrowIfNull(headers);

        if (headers.TryGetValue(NServiceBus.Headers.MessageId, out var originalMessageId) && !string.IsNullOrEmpty(originalMessageId))
        {
            MessageId = originalMessageId;
        }
        else
        {
            MessageId = nativeMessageId;

            headers[NServiceBus.Headers.MessageId] = nativeMessageId;
        }

        NativeMessageId = nativeMessageId;

        Headers = headers;

        Body = body;
    }

    /// <summary>
    /// The message ID.
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// The native message ID.
    /// </summary>
    public string NativeMessageId { get; }

    /// <summary>
    /// The message headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; private set; }

    /// <summary>
    /// Gets/sets a byte array to the body content of the message.
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; private set; }

    /// <summary>
    /// Use this method to update the body of this message.
    /// </summary>
    internal void UpdateBody(ReadOnlyMemory<byte> updatedBody)
    {
        originalBody ??= Body;

        Body = updatedBody;
    }

    /// <summary>
    /// Captures the current headers so they can be reverted later. Only the first call captures; subsequent calls are no-ops.
    /// </summary>
    internal void SnapshotHeaders()
    {
        originalHeaders ??= new Dictionary<string, string>(Headers);
    }

    /// <summary>
    /// Resets body and headers to the exact state as they were when the message was created.
    /// </summary>
    internal void RevertToOriginal()
    {
        if (originalBody != null)
        {
            Body = originalBody.Value;
        }

        if (originalHeaders != null)
        {
            Headers = originalHeaders;
        }
    }

    ReadOnlyMemory<byte>? originalBody;
#nullable enable
    Dictionary<string, string>? originalHeaders;
#nullable restore
}