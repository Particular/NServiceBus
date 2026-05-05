#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using Transport;

abstract class MessageProcessingFailed(string nativeMessageId, string messageId, Dictionary<string, string> headers, ReadOnlyMemory<byte> body, ReceiveProperties receiveProperties, Exception exception)
{
    public string NativeMessageId { get; } = nativeMessageId;
    public string MessageId { get; } = messageId;
    public Dictionary<string, string> Headers { get; } = headers;
    public ReadOnlyMemory<byte> Body { get; } = body;
    public ReceiveProperties ReceiveProperties { get; } = receiveProperties;
    public Exception Exception { get; } = exception;
}