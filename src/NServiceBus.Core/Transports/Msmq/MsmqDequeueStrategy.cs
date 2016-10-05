namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Diagnostics;
    using System.Messaging;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Janitor;
    using NServiceBus.CircuitBreakers;
    using NServiceBus.Logging;
    using NServiceBus.Support;
    using NServiceBus.Unicast.Transport;

    /// <summary>
    ///     Default implementation of <see cref="IDequeueMessages" /> for MSMQ.
    /// </summary>
    public class MsmqDequeueStrategy : IDequeueMessages, IDisposable
    {
        /// <summary>
        ///     Creates an instance of <see cref="MsmqDequeueStrategy" />.
        /// </summary>
        /// <param name="configure">Configure</param>
        /// <param name="criticalError">CriticalError</param>
        /// <param name="unitOfWork">MsmqUnitOfWork</param>
        public MsmqDequeueStrategy(Configure configure, CriticalError criticalError, MsmqUnitOfWork unitOfWork)
        {
            this.configure = configure;
            this.criticalError = criticalError;
            this.unitOfWork = unitOfWork;
        }

        /// <summary>
        ///     Initializes the <see cref="IDequeueMessages" />.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="settings">The <see cref="TransactionSettings" /> to be used by <see cref="IDequeueMessages" />.</param>
        /// <param name="tryProcessMessage">Called when a message has been dequeued and is ready for processing.</param>
        /// <param name="endProcessMessage">
        ///     Needs to be called by <see cref="IDequeueMessages" /> after the message has been
        ///     processed regardless if the outcome was successful or not.
        /// </param>
        public void Init(Address address, TransactionSettings settings, Func<TransportMessage, bool> tryProcessMessage,
            Action<TransportMessage, Exception> endProcessMessage)
        {
            this.tryProcessMessage = tryProcessMessage;
            this.endProcessMessage = endProcessMessage;
            transactionSettings = settings;

            if (address == null)
            {
                throw new ArgumentException("Input queue must be specified");
            }

            if (!address.Machine.Equals(RuntimeEnvironment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                var error = string.Format("Input queue [{0}] must be on the same machine as this process [{1}].", address, RuntimeEnvironment.MachineName);
                throw new InvalidOperationException(error);
            }

            transactionOptions = new TransactionOptions
            {
                IsolationLevel = transactionSettings.IsolationLevel,
                Timeout = transactionSettings.TransactionTimeout
            };

            queue = new MessageQueue(NServiceBus.MsmqUtilities.GetFullPath(address), false, true, QueueAccessMode.Receive);
            errorQueue = new MessageQueue(NServiceBus.MsmqUtilities.GetFullPath(ErrorQueue), false, true, QueueAccessMode.Send);

            if (transactionSettings.IsTransactional && !QueueIsTransactional())
            {
                throw new ArgumentException(
                    "Queue must be transactional if you configure your endpoint to be transactional (" + address + ").");
            }

            var messageReadPropertyFilter = new MessagePropertyFilter
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

            queue.MessageReadPropertyFilter = messageReadPropertyFilter;

            if (configure.PurgeOnStartup())
            {
                queue.Purge();
            }
        }

        /// <summary>
        /// The address of the configured error queue. 
        /// </summary>
        public Address ErrorQueue { get; set; }

        /// <summary>
        ///     Starts the dequeuing of message using the specified <paramref name="maximumConcurrencyLevel" />.
        /// </summary>
        /// <param name="maximumConcurrencyLevel">The maximum concurrency level supported.</param>
        public void Start(int maximumConcurrencyLevel)
        {
            MessageQueue.ClearConnectionCache();

            this.maximumConcurrencyLevel = maximumConcurrencyLevel;
            throttlingSemaphore = new SemaphoreSlim(maximumConcurrencyLevel, maximumConcurrencyLevel);

            queue.PeekCompleted += OnPeekCompleted;

            CallPeekWithExceptionHandling(() => queue.BeginPeek());
        }

        /// <summary>
        ///     Stops the dequeuing of messages.
        /// </summary>
        public void Stop()
        {
            queue.PeekCompleted -= OnPeekCompleted;

            stopResetEvent.WaitOne();
            DrainStopSemaphore();
            queue.Dispose();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            // Injected
        }

        void DrainStopSemaphore()
        {
            Logger.Debug("Drain stopping 'Throttling Semaphore'.");
            for (var index = 0; index < maximumConcurrencyLevel; index++)
            {
                Logger.Debug(string.Format("Claiming Semaphore thread {0}/{1}.", index + 1, maximumConcurrencyLevel));
                throttlingSemaphore.Wait();
            }
            Logger.Debug("Releasing all claimed Semaphore threads.");
            throttlingSemaphore.Release(maximumConcurrencyLevel);

            throttlingSemaphore.Dispose();
        }

        bool QueueIsTransactional()
        {
            try
            {
                return queue.Transactional;
            }
            catch (Exception ex)
            {
                var error = string.Format("There is a problem with the input queue: {0}. See the enclosed exception for details.", queue.Path);
                throw new InvalidOperationException(error, ex);
            }
        }

        void OnPeekCompleted(object sender, PeekCompletedEventArgs peekCompletedEventArgs)
        {
            stopResetEvent.Reset();

            CallPeekWithExceptionHandling(() => queue.EndPeek(peekCompletedEventArgs.AsyncResult));

            throttlingSemaphore.Wait();

            Task.Factory
                .StartNew(Action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .ContinueWith(task => task.Exception.Handle(ex =>
                {
                    Logger.Error("Error processing message.", ex);
                    return true;
                }), TaskContinuationOptions.OnlyOnFaulted);

            //We using an AutoResetEvent here to make sure we do not call another BeginPeek before the Receive has been called
            peekResetEvent.WaitOne();

            CallPeekWithExceptionHandling(() => queue.BeginPeek());

            stopResetEvent.Set();
        }

        void Action()
        {
            TransportMessage transportMessage = null;
            try
            {
                if (transactionSettings.IsTransactional)
                {
                    if (transactionSettings.SuppressDistributedTransactions)
                    {
                        using (var msmqTransaction = new MessageQueueTransaction())
                        {                           
                            Message message;

                            if (!TryReceiveMessage(tx =>
                            {
                                tx.Begin();
                                return queue.Receive(receiveTimeout, tx);
                            }, out message, msmqTransaction))
                            {
                                return;
                            }

                            try
                            {
                                unitOfWork.SetTransaction(msmqTransaction);

                                try
                                {
                                    transportMessage = NServiceBus.MsmqUtilities.Convert(message);
                                }
                                catch (Exception exception)
                                {
                                    LogCorruptedMessage(message, exception);
                                    errorQueue.Send(message, msmqTransaction);
                                    msmqTransaction.Commit();
                                    return;
                                }

                                if (tryProcessMessage(transportMessage))
                                {
                                    msmqTransaction.Commit();
                                }
                                else
                                {
                                    msmqTransaction.Abort();
                                }
                            }
                            finally
                            {
                                unitOfWork.ClearTransaction();
                            }
                        }
                    }
                    else
                    {
                        using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
                        {
                            Message message;

                            if (!TryReceiveMessage(_ => queue.Receive(receiveTimeout, MessageQueueTransactionType.Automatic), out message))
                            {
                                scope.Complete();
                                return;
                            }

                            try
                            {
                                transportMessage = NServiceBus.MsmqUtilities.Convert(message);
                            }
                            catch (Exception ex)
                            {
                                LogCorruptedMessage(message, ex);
                                errorQueue.Send(message, MessageQueueTransactionType.Automatic);
                                scope.Complete();
                                return;
                            }

                            if (tryProcessMessage(transportMessage))
                            {
                                scope.Complete();
                            }
                        }
                    }
                }
                else
                {
                    Message message;

                    if (!TryReceiveMessage(_ => queue.Receive(receiveTimeout, MessageQueueTransactionType.None), out message))
                    {
                        return;
                    }

                    try
                    {
                        transportMessage = NServiceBus.MsmqUtilities.Convert(message);
                    }
                    catch (Exception exception)
                    {
                        LogCorruptedMessage(message, exception);
                        errorQueue.Send(message, MessageQueueTransactionType.None);
                        return;
                    }

                    tryProcessMessage(transportMessage);
                }

                endProcessMessage(transportMessage, null);
            }
            catch (Exception ex)
            {
                endProcessMessage(transportMessage, ex);
            }
            finally
            {
                throttlingSemaphore.Release();
            }
        }

        void LogCorruptedMessage(Message message, Exception ex)
        {
            var error = string.Format("Message '{0}' is corrupt and will be moved to '{1}'", message.Id, ErrorQueue.Queue);
            Logger.Error(error, ex);
        }

        void CallPeekWithExceptionHandling(Action action)
        {
            try
            {
                action();
            }
            catch (MessageQueueException messageQueueException)
            {
                RaiseCriticalException(messageQueueException);
            }
        }

        [DebuggerNonUserCode]
        bool TryReceiveMessage(Func<MessageQueueTransaction, Message> receive, out Message message, MessageQueueTransaction transaction = null)
        {
            message = null;

            try
            {
                message = receive(transaction);
                return true;
            }
            catch (MessageQueueException messageQueueException)
            {
                if (messageQueueException.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    //We should only get an IOTimeout exception here if another process removed the message between us peeking and now.
                    return false;
                }

                RaiseCriticalException(messageQueueException);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in receiving messages.", ex);
            }
            finally
            {
                peekResetEvent.Set();
            }

            return false;
        }

        void RaiseCriticalException(MessageQueueException messageQueueException)
        {
            var errorException = string.Format("Failed to peek messages from [{0}].", queue.FormatName);

            if (messageQueueException.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
            {
                errorException =
                    string.Format(
                        "Do not have permission to access queue [{0}]. Make sure that the current user [{1}] has permission to Send, Receive, and Peek  from this queue.",
                        queue.FormatName, GetUserName());
            }

            circuitBreaker.Execute(() => criticalError.Raise("Error in receiving messages.", new InvalidOperationException(errorException, messageQueueException)));
        }

        static string GetUserName()
        {
            var windowsIdentity = WindowsIdentity.GetCurrent();
            return windowsIdentity != null
                ? windowsIdentity.Name
                : "Unknown User";
        }

        static ILog Logger = LogManager.GetLogger<MsmqDequeueStrategy>();
        Configure configure;
        CriticalError criticalError;
        TimeSpan receiveTimeout = TimeSpan.FromSeconds(1);
        [SkipWeaving]
        MsmqUnitOfWork unitOfWork;
        CircuitBreaker circuitBreaker = new CircuitBreaker(100, TimeSpan.FromSeconds(30));
        Action<TransportMessage, Exception> endProcessMessage;
        int maximumConcurrencyLevel;
        AutoResetEvent peekResetEvent = new AutoResetEvent(false);
        MessageQueue queue;
        MessageQueue errorQueue;
        ManualResetEvent stopResetEvent = new ManualResetEvent(true);
        SemaphoreSlim throttlingSemaphore;
        TransactionOptions transactionOptions;
        TransactionSettings transactionSettings;
        Func<TransportMessage, bool> tryProcessMessage;
    }
}