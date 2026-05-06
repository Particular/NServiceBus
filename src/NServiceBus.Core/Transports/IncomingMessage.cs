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
        : this(nativeMessageId, headers, body, ReceiveProperties.Empty)
    {
    }

    /// <summary>
    /// Creates a new message with receive properties.
    /// </summary>
    /// <param name="nativeMessageId">The native message ID.</param>
    /// <param name="headers">The message headers.</param>
    /// <param name="body">The message body.</param>
    /// <param name="receiveProperties">Properties received from the transport.</param>
    public IncomingMessage(string nativeMessageId, Dictionary<string, string> headers, ReadOnlyMemory<byte> body, ReceiveProperties receiveProperties)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeMessageId);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(receiveProperties);

        NativeMessageId = nativeMessageId;
        MessageId = GetOrSetMessageIdFromHeaders(headers, nativeMessageId);
        Headers = headers;
        Body = body;
        ReceiveProperties = receiveProperties;
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
    public Dictionary<string, string> Headers { get; }

    /// <summary>
    /// Properties received from the transport that can be propagated to outgoing dispatch operations.
    /// </summary>
    public ReceiveProperties ReceiveProperties { get; }

    /// <summary>
    /// Gets/sets a byte array to the body content of the message.
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; private set; }

    /// <summary>
    /// This method is used to ensure that the message ID header is present in the header dictionary.
    /// If the header is already present, it returns its value. If not, it adds the native message ID to the headers and returns it as the message ID.
    /// </summary>
    /// <param name="headers">The message headers to inspects and potentially mutate.</param>
    /// <param name="nativeMessageId">The native message ID.</param>
    /// <returns>The original message ID or the native message ID depending on the availability of the MessageId header.</returns>
    internal static string GetOrSetMessageIdFromHeaders(Dictionary<string, string> headers, string nativeMessageId)
    {
        string messageId;
        if (headers.TryGetValue(NServiceBus.Headers.MessageId, out var originalMessageId) && !string.IsNullOrEmpty(originalMessageId))
        {
            messageId = originalMessageId;
        }
        else
        {
            messageId = nativeMessageId;

            headers[NServiceBus.Headers.MessageId] = nativeMessageId;
        }
        return messageId;
    }

    /// <summary>
    /// Use this method to update the body of this message.
    /// </summary>
    internal void UpdateBody(ReadOnlyMemory<byte> updatedBody)
    {
        originalBody ??= Body;

        Body = updatedBody;
    }

    /// <summary>
    /// Makes sure that the body is reset to the exact state as it was when the message was created.
    /// </summary>
    internal void RevertToOriginalBodyIfNeeded()
    {
        if (originalBody != null)
        {
            Body = originalBody.Value;
        }
    }

    ReadOnlyMemory<byte>? originalBody;
}