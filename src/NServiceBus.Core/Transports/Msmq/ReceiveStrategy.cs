namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Transports;

    abstract class ReceiveStrategy
    {
        public abstract Task ReceiveMessage(CancellationTokenSource cancellationTokenSource);

        public void Init(MessageQueue inputQueue, MessageQueue errorQueue, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<bool>> onError)
        {
            InputQueue = inputQueue;
            ErrorQueue = errorQueue;
            OnMessage = onMessage;
            OnError = onError;
        }

        protected bool TryReceive(Func<MessageQueue, Message> receiveAction, out Message message)
        {
            try
            {
                message = receiveAction(InputQueue);
                return true;
            }
            catch (MessageQueueException ex)
            {
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    //We should only get an IOTimeout exception here if another process removed the message between us peeking and now.
                    message = null;
                    return false;
                }
                throw;
            }
        }

        protected async Task<bool> TryProcessMessage(Message message, Dictionary<string, string> headers, TransportTransaction transaction)
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                using (var bodyStream = message.BodyStream)
                {
                    var pushContext = new MessageContext(message.Id, headers, bodyStream, transaction, tokenSource, new ContextBag());

                    await OnMessage(pushContext).ConfigureAwait(false);
                }

                return tokenSource.Token.IsCancellationRequested;
            }
        }


        protected MessageQueue InputQueue;
        protected MessageQueue ErrorQueue;
        protected Func<MessageContext, Task> OnMessage;
        protected Func<ErrorContext, Task<bool>> OnError;
    }
}