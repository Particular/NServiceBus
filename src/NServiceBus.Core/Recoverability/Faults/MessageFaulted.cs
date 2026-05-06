#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using Transport;

sealed class MessageFaulted(string errorQueue, string nativeMessageId, string messageId, Dictionary<string, string> headers, ReadOnlyMemory<byte> body, ReceiveProperties receiveProperties, Exception exception) : MessageProcessingFailed(nativeMessageId, messageId, headers, body, receiveProperties, exception)
{
    public string ErrorQueue { get; } = errorQueue;
}