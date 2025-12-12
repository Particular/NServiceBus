#nullable enable

namespace NServiceBus;

using System;
using Transport;

abstract class MessageProcessingFailed(IncomingMessage failedMessage, Exception exception)
{
    public IncomingMessage Message { get; } = failedMessage;
    public Exception Exception { get; } = exception;
}