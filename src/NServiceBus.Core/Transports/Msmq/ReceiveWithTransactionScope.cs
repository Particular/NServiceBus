namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using System.Transactions;
    using Transport;

    class ReceiveWithTransactionScope : ReceiveStrategy
    {
        public ReceiveWithTransactionScope(TransactionOptions transactionOptions, MsmqFailureInfoStorage failureInfoStorage)
        {
            this.transactionOptions = transactionOptions;
            this.failureInfoStorage = failureInfoStorage;
        }

        public override async Task ReceiveMessage()
        {
            Message message = null;
            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (!TryReceive(MessageQueueTransactionType.Automatic, out message))
                    {
                        return;
                    }

                    var transportTransaction = new ScopeTransportTransaction(Transaction.Current);

                    Dictionary<string, string> headers;

                    Exception ex;
                    if ((ex = TryExtractHeaders(message, out headers)) != null)
                    {
                        await HandleError(message, new Dictionary<string, string>(), ex, transportTransaction, 1, isPoison: true).ConfigureAwait(false);

                        scope.Complete();
                        return;
                    }

                    var shouldCommit = await ProcessMessage(message, headers, transportTransaction).ConfigureAwait(false);

                    if (!shouldCommit)
                    {
                        return;
                    }

                    scope.Complete();
                }
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

        async Task<bool> ProcessMessage(Message message, Dictionary<string, string> headers, TransportTransaction transportTransaction)
        {
            MsmqFailureInfoStorage.ProcessingFailureInfo failureInfo;

            if (failureInfoStorage.TryGetFailureInfoForMessage(message.Id, out failureInfo))
            {
                var errorHandleResult = await HandleError(message, headers, failureInfo.Exception, transportTransaction, failureInfo.NumberOfProcessingAttempts).ConfigureAwait(false);

                if (errorHandleResult == ErrorHandleResult.Handled)
                {
                    failureInfoStorage.ClearFailureInfoForMessage(message.Id);
                    return true;
                }
            }

            try
            {
                using (var bodyStream = message.BodyStream)
                {
                    var shouldAbortMessageProcessing = await TryProcessMessage(message, headers, bodyStream, transportTransaction).ConfigureAwait(false);

                    if (shouldAbortMessageProcessing)
                    {
                        return false;
                    }
                }

                failureInfoStorage.ClearFailureInfoForMessage(message.Id);
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

        class ScopeTransportTransaction : TransportTransaction
        {
            public ScopeTransportTransaction(Transaction current)
            {
                Set(current);
            }
        }
    }
}