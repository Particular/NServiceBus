namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
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

        public override async Task ReceiveMessage(CancellationTokenSource cancellationTokenSource)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
            {
                Message message;

                if (!TryReceive(queue => InputQueue.Receive(TimeSpan.FromMilliseconds(10), MessageQueueTransactionType.Automatic), out message))
                {
                    return;
                }

                Dictionary<string, string> headers;

                try
                {
                    headers = MsmqUtilities.ExtractHeaders(message);
                }
                catch (Exception ex)
                {
                    var error = $"Message '{message.Id}' is corrupt and will be moved to '{ErrorQueue.QueueName}'";
                    Logger.Error(error, ex);

                    ErrorQueue.Send(message, MessageQueueTransactionType.Automatic);

                    scope.Complete();
                    return;
                }

                var transportTransaction = new TransportTransaction();
                transportTransaction.Set(Transaction.Current);

                var shouldAbort = await TryProcessMessage(message, headers, transportTransaction).ConfigureAwait(false);

                if (!shouldAbort)
                {
                    scope.Complete();
                }
            }
        }

        TransactionOptions transactionOptions;

        static ILog Logger = LogManager.GetLogger<ReceiveWithTransactionScope>();
    }
}