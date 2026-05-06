#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using Transport;

sealed class MessageToBeRetried(int attempt, TimeSpan delay, bool immediateRetry, string nativeMessageId, string messageId, Dictionary<string, string> headers, ReadOnlyMemory<byte> body, ReceiveProperties receiveProperties, Exception exception)
    : MessageProcessingFailed(nativeMessageId, messageId, headers, body, receiveProperties, exception)
{
    public int Attempt { get; } = attempt;
    public TimeSpan Delay { get; } = delay;
    public bool IsImmediateRetry { get; } = immediateRetry;
}