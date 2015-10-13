namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
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

        public override async Task ReceiveMessage(MessageQueue inputQueue, MessageQueue errorQueue, Func<PushContext, Task> onMessage)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
            {
                var message = inputQueue.Receive(TimeSpan.FromMilliseconds(10), MessageQueueTransactionType.Automatic);

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
                    var pushContext = new PushContext(message.Id, headers, bodyStream, new ContextBag());

                    await onMessage(pushContext).ConfigureAwait(false);
                }

                scope.Complete();
            }
        }

        TransactionOptions transactionOptions;

        static ILog Logger = LogManager.GetLogger<ReceiveWithTransactionScope>();
    }
}