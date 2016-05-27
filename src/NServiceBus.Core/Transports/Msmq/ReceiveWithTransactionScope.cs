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

        public override async Task ReceiveMessage(MessageQueue inputQueue, MessageQueue errorQueue, CancellationTokenSource cancellationTokenSource, Func<PushContext, Task> onMessage)
        {
            //this is given to us by the core
            Func<ErrorContext, Task<FaultMetadata>> onError = context => Task.FromResult(new FaultMetadata());

            //this is defaulted by the transport but users will be allowed to override using
            // .UseTransport<MsmqTransport>().RetryPolicy(c=>{....})
            Func<MsmqRecoveryContext, RecoveryAction> determineRecoveryAction = c=> new MsmqImmediateRetry();


            Dictionary<string, string> headers = null;
            Message message = null;

            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
                {
                    message = inputQueue.Receive(TimeSpan.FromMilliseconds(10), MessageQueueTransactionType.Automatic);

                    if (PerformRecoverabilityAction(message))
                    {
                        scope.Complete();
                        return;
                    }

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

                        await onMessage(pushContext).ConfigureAwait(false);
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

                var context = new ErrorContext(ex, message.Id, headers ?? new Dictionary<string, string>());

                //get the fault metadata from the core
                var faultMetadata = await onError(context).ConfigureAwait(false);

                var numberOfRetries = GetNumberOfRetriesFromLRU(message.Id);

                var recoverabilityAction = determineRecoveryAction(new MsmqRecoveryContext(numberOfRetries));

                AddToLRU(message.Id, faultMetadata, recoverabilityAction);
            }
        }

        int GetNumberOfRetriesFromLRU(string messageId)
        {
            //get this from the LRU/headers etc
            return 0;
        }

        void AddToLRU(string messageId, FaultMetadata faultContext, RecoveryAction recoverabilityAction)
        {

        }

        bool PerformRecoverabilityAction(Message message)
        {
            //check LRU to see if we need to do anything, FLR == do nothing, SLR + Error == move it and return true
            return false;
        }


        TransactionOptions transactionOptions;

        static ILog Logger = LogManager.GetLogger<ReceiveWithTransactionScope>();
    }

    class MsmqRecoveryContext
    {
        public int NumberOfImmediateRetriesPerformedByThisEndpointInstance { get; }

        public MsmqRecoveryContext(int numberOfImmediateRetriesPerformedByThisEndpointInstance)
        {
            NumberOfImmediateRetriesPerformedByThisEndpointInstance = numberOfImmediateRetriesPerformedByThisEndpointInstance;
        }
    }

    class MsmqImmediateRetry : RecoveryAction
    {
    }

    class RecoveryAction
    {
    }

    class ErrorContext
    {
        public Exception Exception { get; }
        public string MessageId { get; }
        public Dictionary<string, string> Headers { get; }

        public ErrorContext(Exception exception, string messageId, Dictionary<string, string> headers)
        {
            Exception = exception;
            MessageId = messageId;
            Headers = headers;
        }
    }

    class FaultMetadata
    {
        public Dictionary<string, string> Values { get; set; }
    }
}