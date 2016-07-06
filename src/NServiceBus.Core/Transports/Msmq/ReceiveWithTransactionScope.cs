namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using System.Transactions;
    using Transports;

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
            var transportTransaction = new ScopeTransportTransaction(Transaction.Current);

            MsmqFailureInfoStorage.ProcessingFailureInfo failureInfo;

            if (failureInfoStorage.TryGetFailureInfoForMessage(message.Id, out failureInfo))
            {
                var shouldRetryImmediately = await HandleError(message, headers, failureInfo.Exception, failureInfo.NumberOfProcessingAttempts, transportTransaction).ConfigureAwait(false);

                if (!shouldRetryImmediately)
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