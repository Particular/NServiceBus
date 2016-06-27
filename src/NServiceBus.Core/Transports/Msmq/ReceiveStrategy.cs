namespace NServiceBus
{
    using System;
    using System.Messaging;
    using System.Threading;
    using System.Threading.Tasks;
    using Transports;

    abstract class ReceiveStrategy
    {
        public abstract Task ReceiveMessage(CancellationTokenSource cancellationTokenSource);

        public void Init(MessageQueue inputQueue, MessageQueue errorQueue, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task> onError)
        {
            InputQueue = inputQueue;
            ErrorQueue = errorQueue;
            OnMessage = onMessage;
            OnError = onError;
        }

        protected MessageQueue InputQueue;
        protected MessageQueue ErrorQueue;
        protected Func<MessageContext, Task> OnMessage;
        protected Func<ErrorContext, Task> OnError;
    }
}