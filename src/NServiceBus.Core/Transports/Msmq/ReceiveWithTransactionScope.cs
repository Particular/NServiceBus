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
                var message = InputQueue.Receive(TimeSpan.FromMilliseconds(10), MessageQueueTransactionType.Automatic);

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

                using (var bodyStream = message.BodyStream)
                {
                    var ambientTransaction = new TransportTransaction();
                    ambientTransaction.Set(Transaction.Current);
                    var pushContext = new MessageContext(message.Id, headers, bodyStream, ambientTransaction, cancellationTokenSource, new ContextBag());

                    await OnMessage(pushContext).ConfigureAwait(false);
                }

                if (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    scope.Complete();
                }
            }
        }

        TransactionOptions transactionOptions;

        static ILog Logger = LogManager.GetLogger<ReceiveWithTransactionScope>();
    }
}