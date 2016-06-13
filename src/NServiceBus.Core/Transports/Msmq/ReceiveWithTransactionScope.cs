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
        public ReceiveWithTransactionScope(TransactionOptions transactionOptions, Dictionary<string, Tuple<Exception, int>> failureCache)
        {
            this.transactionOptions = transactionOptions;
            this.failureCache = failureCache;
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
                            var context = new ContextBag();
                            context.Set(ambientTransaction);

                            var errorContext = new ErrorContext(ex, attemptNumber, message.Id, headers, bodyStream, context);

                            var shouldRetryImmediately = await onError(errorContext).ConfigureAwait(false);
                            if (shouldRetryImmediately)
                            {
                                await onMessage(pushContext).ConfigureAwait(false);
                            }
                            else
                            {
                                ClearFailures(message.Id);
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
            //TODO: this is wrong. We need thread-safe structure and better encapsulation here
            if (failureCache.ContainsKey(messageId) == false)
            {
                failureCache.Add(messageId, new Tuple<Exception, int>(exception, 1));
            }
            else
            {
                var previousFailuresData = failureCache[messageId];

                failureCache[messageId] = new Tuple<Exception, int>(exception, previousFailuresData.Item2 + 1);
            }
        }

        void ClearFailures(string messageId)
        {
            //TODO: this is smelly. This method needs to be called before onError because in other case we have a race
            //      condition for short defered retries. But we do not know what recoverability will do with it :/

            failureCache.Remove(messageId);
        }

        bool HasFailedBefore(string messageId, out Exception exception, out int attemptNumber)
        {
            Tuple<Exception, int> failureRecord;
            if (failureCache.TryGetValue(messageId, out failureRecord))
            {
                exception = failureRecord.Item1;
                attemptNumber = failureRecord.Item2;

                return true;
            }

            exception = null;
            attemptNumber = 0;

            return false;
        }


        TransactionOptions transactionOptions;
        readonly Dictionary<string, Tuple<Exception, int>> failureCache;

        static ILog Logger = LogManager.GetLogger<ReceiveWithTransactionScope>();
    }
}