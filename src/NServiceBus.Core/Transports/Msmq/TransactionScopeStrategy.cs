namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using System.Transactions;
    using Transport;

    class TransactionScopeStrategy : ReceiveStrategy
    {
        public TransactionScopeStrategy(TransactionOptions transactionOptions, MsmqFailureInfoStorage failureInfoStorage)
        {
            this.transactionOptions = transactionOptions;
            this.failureInfoStorage = failureInfoStorage;
        }

        public override async Task ReceiveMessage()
        {
            Message message = null;
            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (!TryReceive(MessageQueueTransactionType.Automatic, out message))
                    {
                        return;
                    }

                    Dictionary<string, string> headers;

                    if (!TryExtractHeaders(message, out headers))
                    {
                        MovePoisonMessageToErrorQueue(message, MessageQueueTransactionType.Automatic);

                        scope.Complete();
                        return;
                    }

                    var shouldCommit = await ProcessMessage(message, headers).ConfigureAwait(false);

                    if (!shouldCommit)
                    {
                        return;
                    }

                    scope.Complete();
                }

                failureInfoStorage.ClearFailureInfoForMessage(message.Id);
            }
            // We'll only get here if Complete/Dispose throws which should be rare.
            // Note: If that happens the attempts counter will be inconsistent since the message might be picked up again before we can register the failure in the LRU cache.
            catch (Exception exception)
            {
                if (message == null)
                {
                    throw;
                }

                failureInfoStorage.RecordFailureInfoForMessage(message.Id, exception);
            }
        }

        async Task<bool> ProcessMessage(Message message, Dictionary<string, string> headers)
        {
            var transportTransaction = new TransportTransaction();
            transportTransaction.Set(Transaction.Current);

            MsmqFailureInfoStorage.ProcessingFailureInfo failureInfo;

            if (failureInfoStorage.TryGetFailureInfoForMessage(message.Id, out failureInfo))
            {
                var errorHandleResult = await HandleError(message, headers, failureInfo.Exception, transportTransaction, failureInfo.NumberOfProcessingAttempts).ConfigureAwait(false);

                if (errorHandleResult == ErrorHandleResult.Handled)
                {
                    return true;
                }
            }

            try
            {
                using (var bodyStream = message.BodyStream)
                {
                    var shouldAbortMessageProcessing = await TryProcessMessage(message.Id, headers, bodyStream, transportTransaction).ConfigureAwait(false);

                    if (shouldAbortMessageProcessing)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception exception)
            {
                failureInfoStorage.RecordFailureInfoForMessage(message.Id, exception);
                return false;
            }
        }

        TransactionOptions transactionOptions;
        MsmqFailureInfoStorage failureInfoStorage;
    }
}