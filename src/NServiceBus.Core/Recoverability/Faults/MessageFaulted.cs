#nullable enable

namespace NServiceBus;

using System;
using Transport;

sealed class MessageFaulted(string errorQueue, IncomingMessage failedMessage, Exception exception) : MessageProcessingFailed(failedMessage, exception)
{
    public string ErrorQueue { get; } = errorQueue;
}