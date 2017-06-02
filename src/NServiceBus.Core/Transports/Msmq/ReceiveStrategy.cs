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

        public void Init(MessageQueue inputQueue, MessageQueue errorQueue, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CriticalError criticalError)
        {
            this.inputQueue = inputQueue;
            this.errorQueue = errorQueue;
            this.onMessage = onMessage;
            this.onError = onError;
            this.criticalError = criticalError;
        }

        protected bool TryReceive(MessageQueueTransactionType transactionType, out Message message)
        {
            try
            {
                Logger.Info($"InputQueue.Receive() w/ transactionType={transactionType}");
                message = inputQueue.Receive(TimeSpan.FromMilliseconds(10), transactionType);
                Logger.Info($"InputQueue.Receive() complete, GotMessage={message?.Id ?? "NULL"}");

                return true;
            }
            catch (MessageQueueException ex)
            {
                Logger.Info($"TryReceive by transactionType got MessageQueueException, Code = {ex.MessageQueueErrorCode}");
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
                Logger.Info($"InputQueue.Receive() w/ transaction, txStatus={transaction.Status}");
                message = inputQueue.Receive(TimeSpan.FromMilliseconds(10), transaction);
                Logger.Info($"InputQueue.Receive() complete, GotMessage={message?.Id ?? "NULL"}");

                return true;
            }
            catch (MessageQueueException ex)
            {
                Logger.Info($"TryReceive by transaction got MessageQueueException, Code = {ex.MessageQueueErrorCode}");
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    //We should only get an IOTimeout exception here if another process removed the message between us peeking and now.
                    message = null;
                    return false;
                }
                throw;
            }
        }

        protected bool TryExtractHeaders(Message message, out Dictionary<string, string> headers)
        {
            try
            {
                headers = MsmqUtilities.ExtractHeaders(message);
                return true;
            }
            catch (Exception ex)
            {
                var error = $"Message '{message.Id}' has corrupted headers";

                Logger.Warn(error, ex);

                headers = null;
                return false;
            }
        }

        protected void MovePoisonMessageToErrorQueue(Message message, MessageQueueTransaction transaction)
        {
            var error = $"Message '{message.Id}' is classfied as a poison message and will be moved to '{errorQueue.QueueName}'";

            Logger.Error(error);

            errorQueue.Send(message, transaction);
        }

        protected void MovePoisonMessageToErrorQueue(Message message, MessageQueueTransactionType transactionType)
        {
            var error = $"Message '{message.Id}' is classfied as a poison message and will be moved to '{errorQueue.QueueName}'";

            Logger.Error(error);

            errorQueue.Send(message, transactionType);
        }

        protected async Task<bool> TryProcessMessage(string messageId, Dictionary<string, string> headers, Stream bodyStream, TransportTransaction transaction)
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var body = await ReadStream(bodyStream).ConfigureAwait(false);
                var messageContext = new MessageContext(messageId, headers, body, transaction, tokenSource, new ContextBag());

                Logger.Info($"TryProcessMessage - calling onMessage(msgId {messageContext.MessageId})");
                await onMessage(messageContext).ConfigureAwait(false);
                Logger.Info($"TryProcessMessage - onMessage(msgId {messageContext.MessageId}) complete");

                return tokenSource.Token.IsCancellationRequested;
            }
        }

        protected async Task<ErrorHandleResult> HandleError(Message message, Dictionary<string, string> headers, Exception exception, TransportTransaction transportTransaction, int processingAttempts)
        {
            try
            {
                var body = await ReadStream(message.BodyStream).ConfigureAwait(false);
                var errorContext = new ErrorContext(exception, headers, message.Id, body, transportTransaction, processingAttempts);

                return await onError(errorContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                criticalError.Raise($"Failed to execute recoverability actions for message `{message.Id}`", ex);

                //best thing we can do is roll the message back if possible
                return ErrorHandleResult.RetryRequired;
            }
        }

        static async Task<byte[]> ReadStream(Stream bodyStream)
        {
            bodyStream.Seek(0, SeekOrigin.Begin);
            var length = (int) bodyStream.Length;
            var body = new byte[length];
            await bodyStream.ReadAsync(body, 0, length).ConfigureAwait(false);
            return body;
        }

        protected bool IsQueuesTransactional => errorQueue.Transactional;

        MessageQueue inputQueue;
        MessageQueue errorQueue;
        Func<MessageContext, Task> onMessage;
        Func<ErrorContext, Task<ErrorHandleResult>> onError;
        CriticalError criticalError;

        static ILog Logger = LogManager.GetLogger<ReceiveStrategy>();
    }
}