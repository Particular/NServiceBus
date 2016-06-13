namespace NServiceBus
{
    using System;
    using System.Messaging;
    using System.Threading;
    using System.Threading.Tasks;
    using Transports;

    abstract class ReceiveStrategy
    {
        public abstract Task ReceiveMessage(MessageQueue inputQueue, MessageQueue errorQueue, CancellationTokenSource cancellationTokenSource, Func<PushContext, Task> onMessage, Func<ErrorContext, Task<bool>> onError);
    }
}