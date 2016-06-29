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

                    MsmqFailureInfoStorage.ProcessingFailureInfo failureInfo;

                    if (failureInfoStorage.TryGetFailureInfoForMessage(message.Id, out failureInfo))
                    {
                        var shouldRetryImmediately = await HandleError(message, headers, failureInfo.Exception, failureInfo.NumberOfProcessingAttempts).ConfigureAwait(false);

                        if (!shouldRetryImmediately)
                        {
                            failureInfoStorage.ClearFailureInfoForMessage(message.Id);
                            scope.Complete();
                            return;
                        }
                    }
                    var shouldAbortMessageProcessing = await TryProcessMessage(message, headers, new ScopeTransportTransaction(Transaction.Current)).ConfigureAwait(false);

                    if (!shouldAbortMessageProcessing)
                    {
                        failureInfoStorage.ClearFailureInfoForMessage(message.Id);
                        scope.Complete();
                    }
                }
            }
            catch (Exception exception)
            {
                if (message == null)
                {
                    throw;
                }

                failureInfoStorage.RecordFailureInfoForMessage(message.Id, exception);
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