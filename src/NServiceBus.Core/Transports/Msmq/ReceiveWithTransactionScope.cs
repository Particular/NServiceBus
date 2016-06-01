namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Messaging;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using Logging;
    using Transports;

    class ReceiveWithTransactionScope : ReceiveStrategy
    {
        public ReceiveWithTransactionScope(TransactionOptions transactionOptions)
        {
            this.transactionOptions = transactionOptions;
        }

        public override async Task ReceiveMessage(MessageQueue inputQueue, MessageQueue errorQueue, CancellationTokenSource cancellationTokenSource, Func<PushContext, Task> onMessage, Func<ErrorContext, Task<bool>> onError)
        {
            Message message = null;

            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
                {
                    message = inputQueue.Receive(TimeSpan.FromMilliseconds(10), MessageQueueTransactionType.Automatic);

                    Dictionary<string, string> headers;

                    try
                    {
                        headers = MsmqUtilities.ExtractHeaders(message);
                    }
                    catch (Exception ex)
                    {
                        var error = $"Message '{message.Id}' is corrupt and will be moved to '{errorQueue.QueueName}'";
                        Logger.Error(error, ex);

                        errorQueue.Send(message, MessageQueueTransactionType.Automatic);

                        scope.Complete();
                        return;
                    }

                    using (var bodyStream = message.BodyStream)
                    {
                        var ambientTransaction = new TransportTransaction();
                        ambientTransaction.Set(Transaction.Current);
                        var pushContext = new PushContext(message.Id, headers, bodyStream, ambientTransaction, cancellationTokenSource, new ContextBag());

                        Exception ex;
                        int attemptNumber;
                        if (!HasFailedBefore(message.Id, out ex, out attemptNumber))
                        {
                            await onMessage(pushContext).ConfigureAwait(false);
                        }
                        else
                        {
                            var shouldRetryImmediately = await onError(new ErrorContext(ex, attemptNumber, ambientTransaction, message.Id, headers, bodyStream)).ConfigureAwait(false);
                            if (shouldRetryImmediately)
                            {
                                await onMessage(pushContext).ConfigureAwait(false);
                            }
                        }
                    }

                    if (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        scope.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                if (message == null)
                {
                    throw;
                }
                RecordException(message.Id, ex);
            }
        }

        void RecordException(string messageId, Exception exception)
        {
        }

        bool HasFailedBefore(string messageId, out Exception exception, out int attemptNumber)
        {
            throw new NotImplementedException();
        }
        

        TransactionOptions transactionOptions;

        static ILog Logger = LogManager.GetLogger<ReceiveWithTransactionScope>();
    }
}