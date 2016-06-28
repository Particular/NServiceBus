namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using System.Transactions;
    using Transports;

    class ReceiveWithTransactionScope : ReceiveStrategy
    {
        public ReceiveWithTransactionScope(TransactionOptions transactionOptions)
        {
            this.transactionOptions = transactionOptions;
        }

        public override async Task ReceiveMessage()
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
            {
                Message message;

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

                var transaction = new ScopeTransportTransaction(Transaction.Current);

                var shouldAbort = await TryProcessMessage(message, headers, transaction).ConfigureAwait(false);

                if (!shouldAbort)
                {
                    scope.Complete();
                }
            }
        }

        TransactionOptions transactionOptions;

        class ScopeTransportTransaction : TransportTransaction
        {
            public ScopeTransportTransaction(Transaction current)
            {
                Set(current);
            }
        }
    }
}