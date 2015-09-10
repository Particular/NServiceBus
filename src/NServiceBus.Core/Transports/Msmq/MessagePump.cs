namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Messaging;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.CircuitBreakers;
    using NServiceBus.Logging;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class MessagePump : IPushMessages, IDisposable
    {
        public MessagePump(CriticalError criticalError)
        {
            this.criticalError = criticalError;
        }

        public DequeueInfo Init(Func<PushContext, Task> pipe, PushSettings settings)
        {
            pipeline = pipe;

            receiveStrategy = SelectReceiveStrategy(settings.TransactionSettings);

            peekCircuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("MsmqPeek", TimeSpan.FromSeconds(30), ex => criticalError.Raise("Failed to peek " + settings.InputQueue, ex));
            receiveCircuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("MsmqReceive", TimeSpan.FromSeconds(30), ex => criticalError.Raise("Failed to receive from " + settings.InputQueue, ex));

            var msmqAddress = MsmqAddress.Parse(settings.InputQueue);

            var queueName = msmqAddress.Queue;
            if (!string.Equals(msmqAddress.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                var message = string.Format("MSMQ Dequeuing can only run against the local machine. Invalid inputQueue name '{0}'", settings.InputQueue);
                throw new Exception(message);
            }

            inputQueue = new MessageQueue(MsmqUtilities.GetFullPath(queueName), false, true, QueueAccessMode.Receive);
            errorQueue = new MessageQueue(MsmqUtilities.GetFullPath(settings.ErrorQueue), false, true, QueueAccessMode.Send);

            if (settings.TransactionSettings.IsTransactional && !QueueIsTransactional())
            {
                throw new ArgumentException("Queue must be transactional if you configure your endpoint to be transactional (" + settings.InputQueue + ").");
            }

            inputQueue.MessageReadPropertyFilter = DefaultReadPropertyFilter;

            if (settings.PurgeOnStartup)
            {
                inputQueue.Purge();
            }
            return new DequeueInfo(queueName);

        }

        /// <summary>
        ///     Starts the dequeuing of message using the specified
        /// </summary>
        public void Start(PushRuntimeSettings limitations)
        {
            MessageQueue.ClearConnectionCache();

            runningReceiveTasks = new ConcurrentDictionary<Task, Task>();
            concurrencyLimiter = new SemaphoreSlim(limitations.MaxConcurrency);
            cancellationTokenSource = new CancellationTokenSource();

            cancellationToken = cancellationTokenSource.Token;
            messagePumpTask = Task.Factory.StartNew(() => ProcessMessages(), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        /// <summary>
        ///     Stops the dequeuing of messages.
        /// </summary>
        public async Task Stop()
        {
            cancellationTokenSource.Cancel();

            // ReSharper disable once MethodSupportsCancellation
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            var allTasks = runningReceiveTasks.Values.Concat(new[] { messagePumpTask });
            var finishedTask = await Task.WhenAny(Task.WhenAll(allTasks), timeoutTask).ConfigureAwait(false);

            if (finishedTask.Equals(timeoutTask))
            {
                Logger.Error("The message pump failed to stop with in the time allowed(30s)");
            }

            concurrencyLimiter.Dispose();
            runningReceiveTasks.Clear();
            inputQueue.Dispose();
            errorQueue.Dispose();
        }

        public void Dispose()
        {
            // Injected
        }

        [DebuggerNonUserCode]
        async Task ProcessMessages()
        {
            try
            {
                await InnerProcessMessages().ConfigureAwait(false);
            }
            catch(OperationCanceledException)
            {
                // For graceful shutdown purposes
            }
            catch (Exception ex)
            {
                Logger.Error("MSMQ Message pump failed", ex);
                peekCircuitBreaker.Failure(ex);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                await ProcessMessages().ConfigureAwait(false);
            }
        }

        async Task InnerProcessMessages()
        {
            using (var enumerator = inputQueue.GetMessageEnumerator2())
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        //note: .Peek will throw an ex if no message is available. It also turns out that .MoveNext is faster since message isn't read
                        if (!enumerator.MoveNext(TimeSpan.FromMilliseconds(10)))
                        {
                            continue;
                        }

                        peekCircuitBreaker.Success();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("MSMQ receive operation failed", ex);
                        peekCircuitBreaker.Failure(ex);
                        continue;
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    await concurrencyLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            await receiveStrategy.ReceiveMessage(inputQueue, errorQueue, pipeline).ConfigureAwait(false);

                            receiveCircuitBreaker.Success();
                        }
                        catch (MessageProcessingAbortedException)
                        {
                            //expected to happen
                        }
                        catch (MessageQueueException ex)
                        {
                            if (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                            {
                                //We should only get an IOTimeout exception here if another process removed the message between us peeking and now.
                                return;
                            }

                            Logger.Warn("MSMQ receive operation failed", ex);
                            receiveCircuitBreaker.Failure(ex);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn("MSMQ receive operation failed", ex);
                            receiveCircuitBreaker.Failure(ex);
                        }
                        finally
                        {
                            concurrencyLimiter.Release();
                        }
                    }, cancellationToken);

                    task.ContinueWith(t =>
                    {
                        Task wayne;
                        runningReceiveTasks.TryRemove(t, out wayne);
                    }, TaskContinuationOptions.ExecuteSynchronously)
                    .Ignore();

                    runningReceiveTasks.AddOrUpdate(task, task, (k, v) => task)
                        .Ignore();
                }
            }
        }

        bool QueueIsTransactional()
        {
            try
            {
                return inputQueue.Transactional;
            }
            catch (Exception ex)
            {
                var error = string.Format("There is a problem with the input inputQueue: {0}. See the enclosed exception for details.", inputQueue.Path);
                throw new InvalidOperationException(error, ex);
            }
        }

        ReceiveStrategy SelectReceiveStrategy(TransactionSettings transactionSettings)
        {
            if (!transactionSettings.IsTransactional)
            {
                return new ReceiveWithNoTransaction();
            }

            if (transactionSettings.SuppressDistributedTransactions)
            {
                return new ReceiveWithNativeTransaction();
            }


            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = transactionSettings.IsolationLevel,
                Timeout = transactionSettings.TransactionTimeout
            };

            return new ReceiveWithTransactionScope(transactionOptions);
        }


        Task messagePumpTask;
        ConcurrentDictionary<Task, Task> runningReceiveTasks;
        SemaphoreSlim concurrencyLimiter;
        CancellationTokenSource cancellationTokenSource;
        CancellationToken cancellationToken;
        Func<PushContext, Task> pipeline;
        ReceiveStrategy receiveStrategy;
        CriticalError criticalError;
        RepeatedFailuresOverTimeCircuitBreaker peekCircuitBreaker;
        RepeatedFailuresOverTimeCircuitBreaker receiveCircuitBreaker;
        MessageQueue errorQueue;
        MessageQueue inputQueue;

        static ILog Logger = LogManager.GetLogger<ReceiveWithNativeTransaction>();

        static MessagePropertyFilter DefaultReadPropertyFilter = new MessagePropertyFilter
        {
            Body = true,
            TimeToBeReceived = true,
            Recoverable = true,
            Id = true,
            ResponseQueue = true,
            CorrelationId = true,
            Extension = true,
            AppSpecific = true
        };
    }
}