namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Diagnostics;
    using System.Messaging;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.CircuitBreakers;
    using NServiceBus.Logging;

    class MessagePump : IPushMessages, IDisposable
    {
        public MessagePump(CriticalError criticalError)
        {
            this.criticalError = criticalError;
        }

        public DequeueInfo Init(Action<PushContext> pipe, PushSettings settings)
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

            if (limitations.MaxConcurrency.HasValue)
            {
                scheduler = new LimitedConcurrencyLevelTaskScheduler(limitations.MaxConcurrency.Value);
            }
            else
            {
                scheduler = TaskScheduler.Current;
            }
            cancellationTokenSource = new CancellationTokenSource();

            messagePumpTask = Task.Factory.StartNew(_ => { ProcessMessages(); }, cancellationTokenSource.Token)
                      .ContinueWith(t =>
                         {
                             Logger.Error("MSMQ Message pump failed", t.Exception);

                             if (!cancellationTokenSource.IsCancellationRequested)
                             {
                                 ProcessMessages();
                             }
                         }, TaskContinuationOptions.OnlyOnFaulted);
        }



        /// <summary>
        ///     Stops the dequeuing of messages.
        /// </summary>
        public void Stop()
        {
            cancellationTokenSource.Cancel();

            try
            {
                if (!messagePumpTask.Wait(TimeSpan.FromSeconds(30)))
                {
                    Logger.Error("The message pump failed to stop with in the time allowed(30s)");
                }
            }
            catch (AggregateException aex)
            {
                aex.Handle(ex =>
                {
                    if (ex is OperationCanceledException)
                    {
                        return true;
                    }

                    Logger.Error("Exceptions when stopping message pump", ex);
                    return true;
                });
            }

            inputQueue.Dispose();
            errorQueue.Dispose();
        }

        public void Dispose()
        {
            // Injected
        }

        [DebuggerNonUserCode]
        void ProcessMessages()
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
                        peekCircuitBreaker.Failure(ex);
                        continue;
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            receiveStrategy.ReceiveMessage(inputQueue, errorQueue, pipeline);

                            receiveCircuitBreaker.Success();
                        }
                        catch (MessageProcessingAbortedException)
                        {
                            //expected to happen
                        }
                        catch (MessageQueueException messageQueueException)
                        {
                            if (messageQueueException.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                            {
                                //We should only get an IOTimeout exception here if another process removed the message between us peeking and now.
                                return;
                            }

                            throw;
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn("MSMQ receive operation failed", ex);
                            receiveCircuitBreaker.Failure(ex);
                        }
                    }, cancellationTokenSource.Token, TaskCreationOptions.AttachedToParent, scheduler);
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

        ReceiveStrategy SelectReceiveStrategy(Unicast.Transport.TransactionSettings transactionSettings)
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
        TaskScheduler scheduler;
        CancellationTokenSource cancellationTokenSource;
        Action<PushContext> pipeline;
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