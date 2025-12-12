#nullable enable

namespace NServiceBus;

using System;
using Transport;

sealed class MessageToBeRetried(int attempt, TimeSpan delay, bool immediateRetry, IncomingMessage failedMessage, Exception exception)
    : MessageProcessingFailed(failedMessage, exception)
{
    public int Attempt { get; } = attempt;
    public TimeSpan Delay { get; } = delay;
    public bool IsImmediateRetry { get; } = immediateRetry;
}