namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Messaging;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Support;
    using Transport;

    class MessagePump : IPushMessages, IDisposable
    {
        public MessagePump(Func<TransportTransactionMode, ReceiveStrategy> receiveStrategyFactory)
        {
            this.receiveStrategyFactory = receiveStrategyFactory;
        }

        public void Dispose()
        {
            // Injected
        }


        public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CriticalError criticalError, PushSettings settings)
        {
            peekCircuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("MsmqPeek", TimeSpan.FromSeconds(30), ex => criticalError.Raise("Failed to peek " + settings.InputQueue, ex));
            receiveCircuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("MsmqReceive", TimeSpan.FromSeconds(30), ex => criticalError.Raise("Failed to receive from " + settings.InputQueue, ex));

            var inputAddress = MsmqAddress.Parse(settings.InputQueue);
            var errorAddress = MsmqAddress.Parse(settings.ErrorQueue);

            if (!string.Equals(inputAddress.Machine, RuntimeEnvironment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"MSMQ Dequeuing can only run against the local machine. Invalid inputQueue name '{settings.InputQueue}'.");
            }

            inputQueue = new MessageQueue(inputAddress.FullPath, false, true, QueueAccessMode.Receive);
            errorQueue = new MessageQueue(errorAddress.FullPath, false, true, QueueAccessMode.Send);

            if (settings.RequiredTransactionMode != TransportTransactionMode.None && !QueueIsTransactional())
            {
                throw new ArgumentException($"Queue must be transactional if you configure the endpoint to be transactional ({settings.InputQueue}).");
            }

            inputQueue.MessageReadPropertyFilter = DefaultReadPropertyFilter;

            if (settings.PurgeOnStartup)
            {
                inputQueue.Purge();
            }

            receiveStrategy = receiveStrategyFactory(settings.RequiredTransactionMode);

            receiveStrategy.Init(inputQueue, errorQueue, onMessage, onError, criticalError);

            return TaskEx.CompletedTask;
        }

        public void Start(PushRuntimeSettings limitations)
        {
            MessageQueue.ClearConnectionCache();

            runningReceiveTasks = new ConcurrentDictionary<Task, Task>();
            concurrencyLimiter = new SemaphoreSlim(limitations.MaxConcurrency);
            cancellationTokenSource = new CancellationTokenSource();

            cancellationToken = cancellationTokenSource.Token;
            // ReSharper disable once ConvertClosureToMethodGroup
            // LongRunning is useless combined with async/await
            messagePumpTask = Task.Run(() => ProcessMessages(), CancellationToken.None);
        }

        public async Task Stop()
        {
            cancellationTokenSource.Cancel();

            // ReSharper disable once MethodSupportsCancellation
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            var allTasks = runningReceiveTasks.Values.Concat(new[]
            {
                messagePumpTask
            });
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

        [DebuggerNonUserCode]
        async Task ProcessMessages()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await InnerProcessMessages().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // For graceful shutdown purposes
                }
                catch (Exception ex)
                {
                    Logger.Error("MSMQ Message pump failed", ex);
                    await peekCircuitBreaker.Failure(ex).ConfigureAwait(false);
                }
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
                        await peekCircuitBreaker.Failure(ex).ConfigureAwait(false);
                        continue;
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    await concurrencyLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);

                    var receiveTask = ReceiveMessage();

                    runningReceiveTasks.TryAdd(receiveTask, receiveTask);

                    // We insert the original task into the runningReceiveTasks because we want to await the completion
                    // of the running receives. ExecuteSynchronously is a request to execute the continuation as part of
                    // the transition of the antecedents completion phase. This means in most of the cases the continuation
                    // will be executed during this transition and the antecedent task goes into the completion state only
                    // after the continuation is executed. This is not always the case. When the TPL thread handling the
                    // antecedent task is aborted the continuation will be scheduled. But in this case we don't need to await
                    // the continuation to complete because only really care about the receive operations. The final operation
                    // when shutting down is a clear of the running tasks anyway.
                    receiveTask.ContinueWith((t, state) =>
                    {
                        var receiveTasks = (ConcurrentDictionary<Task, Task>) state;
                        Task toBeRemoved;
                        receiveTasks.TryRemove(t, out toBeRemoved);
                    }, runningReceiveTasks, TaskContinuationOptions.ExecuteSynchronously)
                        .Ignore();
                }
            }
        }

        Task ReceiveMessage()
        {
            return TaskEx.Run(async state =>
            {
                var messagePump = (MessagePump) state;

                try
                {
                    await messagePump.receiveStrategy.ReceiveMessage().ConfigureAwait(false);
                    messagePump.receiveCircuitBreaker.Success();
                }
                catch (OperationCanceledException)
                {
                    // Intentionally ignored
                }
                catch (Exception ex)
                {
                    Logger.Warn("MSMQ receive operation failed", ex);
                    await messagePump.receiveCircuitBreaker.Failure(ex).ConfigureAwait(false);
                }
                finally
                {
                    messagePump.concurrencyLimiter.Release();
                }
            }, this);
        }

        bool QueueIsTransactional()
        {
            try
            {
                return inputQueue.Transactional;
            }
            catch (Exception ex)
            {
                var error = $"There is a problem with the input inputQueue: {inputQueue.Path}. See the enclosed exception for details.";
                throw new InvalidOperationException(error, ex);
            }
        }

        CancellationToken cancellationToken;
        CancellationTokenSource cancellationTokenSource;
        SemaphoreSlim concurrencyLimiter;
        MessageQueue errorQueue;
        MessageQueue inputQueue;

        Task messagePumpTask;

        ReceiveStrategy receiveStrategy;

        RepeatedFailuresOverTimeCircuitBreaker peekCircuitBreaker;
        RepeatedFailuresOverTimeCircuitBreaker receiveCircuitBreaker;
        Func<TransportTransactionMode, ReceiveStrategy> receiveStrategyFactory;
        ConcurrentDictionary<Task, Task> runningReceiveTasks;

        static ILog Logger = LogManager.GetLogger<MessagePump>();

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