namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Messaging;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Transport;

    abstract class ReceiveStrategy
    {
        public abstract Task ReceiveMessage();

        public void Init(MessageQueue inputQueue, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CriticalError criticalError)
        {
            this.inputQueue = inputQueue;
            this.onMessage = onMessage;
            this.onError = onError;
            this.criticalError = criticalError;
        }

        protected bool TryReceive(MessageQueueTransactionType transactionType, out Message message)
        {
            try
            {
                message = inputQueue.Receive(TimeSpan.FromMilliseconds(10), transactionType);

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
                message = inputQueue.Receive(TimeSpan.FromMilliseconds(10), transaction);

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

        protected Exception TryExtractHeaders(Message message, out Dictionary<string, string> headers)
        {
            try
            {
                headers = MsmqUtilities.ExtractHeaders(message);
                return null;
            }
            catch (Exception ex)
            {
                var error = $"Message '{message.Id}' has corrupted headers";

                Logger.Warn(error, ex);

                headers = null;
                return ex;
            }
        }

        protected async Task<bool> TryProcessMessage(Message message, Dictionary<string, string> headers, Stream bodyStream, TransportTransaction transaction)
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var messageContext = new MessageContext(message.Id, headers, bodyStream, transaction, tokenSource, new ContextBag());

                await onMessage(messageContext).ConfigureAwait(false);

                return tokenSource.Token.IsCancellationRequested;
            }
        }

        protected async Task<ErrorHandleResult> HandleError(Message message, Dictionary<string, string> headers, Exception exception, TransportTransaction transportTransaction, int processingAttempts, bool isPoison = false)
        {
            try
            {
                var errorContext = new ErrorContext(exception, headers, message.Id, message.BodyStream, transportTransaction, processingAttempts, isPoison);

                return await onError(errorContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                criticalError.Raise($"Failed to execute reverability actions for message `{message.Id}`", ex);

                //best thing we can do is roll the message back if possible
                return ErrorHandleResult.RetryRequired;
            }
        }

        MessageQueue inputQueue;
        Func<MessageContext, Task> onMessage;
        Func<ErrorContext, Task<ErrorHandleResult>> onError;
        CriticalError criticalError;

        static ILog Logger = LogManager.GetLogger<ReceiveStrategy>();
    }
}