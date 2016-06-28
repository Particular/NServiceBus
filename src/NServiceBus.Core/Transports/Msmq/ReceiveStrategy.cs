namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
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

        protected bool TryReceive(MessageQueueTransactionType transactionType, out Message message)
        {
            try
            {
                message = InputQueue.Receive(TimeSpan.FromMilliseconds(10), transactionType);

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

        protected bool TryReceive(MessageQueueTransaction transaction, out Message message)
        {
            try
            {
                message = InputQueue.Receive(TimeSpan.FromMilliseconds(10), transaction);

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

        protected bool TryExtractHeaders(Message message, MessageQueueTransactionType transactionType, out Dictionary<string, string> headers)
        {
            try
            {
                headers = MsmqUtilities.ExtractHeaders(message);
                return true;
            }
            catch (Exception ex)
            {
                var error = $"Message '{message.Id}' is corrupt and will be moved to '{ErrorQueue.QueueName}'";
                Logger.Error(error, ex);

                ErrorQueue.Send(message, transactionType);

                headers = null;
                return false;
            }
        }

        protected bool TryExtractHeaders(Message message, MessageQueueTransaction transaction, out Dictionary<string, string> headers)
        {
            try
            {
                headers = MsmqUtilities.ExtractHeaders(message);
                return true;
            }
            catch (Exception ex)
            {
                var error = $"Message '{message.Id}' is corrupt and will be moved to '{ErrorQueue.QueueName}'";
                Logger.Error(error, ex);

                ErrorQueue.Send(message, transaction);

                headers = null;
                return false;
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

        static ILog Logger = LogManager.GetLogger<ReceiveStrategy>();

    }
}