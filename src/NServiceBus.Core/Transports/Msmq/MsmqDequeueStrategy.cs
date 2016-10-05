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
        static ILog Logger = LogManager.GetLogger<MsmqDequeueStrategy>();

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
        ///     The address of the configured error queue.
        /// </summary>
        public Address ErrorQueue { get; set; }

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

            receiveQueue = new MessageQueue(NServiceBus.MsmqUtilities.GetFullPath(address), false, true, QueueAccessMode.Receive);
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

            receiveQueue.MessageReadPropertyFilter = messageReadPropertyFilter;

            if (configure.PurgeOnStartup())
            {
                receiveQueue.Purge();
            }
        }

        /// <summary>
        ///     Starts the dequeuing of message using the specified <paramref name="maximumConcurrencyLevel" />.
        /// </summary>
        /// <param name="maximumConcurrencyLevel">The maximum concurrency level supported.</param>
        public void Start(int maximumConcurrencyLevel)
        {
            MessageQueue.ClearConnectionCache();

            this.maximumConcurrencyLevel = maximumConcurrencyLevel;
            throttlingSemaphore = new SemaphoreSlim(maximumConcurrencyLevel, maximumConcurrencyLevel);

            receiveQueue.PeekCompleted += OnPeekCompleted;

            CallPeekWithExceptionHandling((queue, _) => queue.BeginPeek(), receiveQueue, circuitBreaker, criticalError);
        }

        /// <summary>
        ///     Stops the dequeuing of messages.
        /// </summary>
        public void Stop()
        {
            receiveQueue.PeekCompleted -= OnPeekCompleted;

            stopResetEvent.WaitOne();
            DrainStopSemaphore();
            receiveQueue.Dispose();
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
                Logger.Debug($"Claiming Semaphore thread {index + 1}/{maximumConcurrencyLevel}.");
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
                return receiveQueue.Transactional;
            }
            catch (Exception ex)
            {
                var error = $"There is a problem with the input queue: {receiveQueue.Path}. See the enclosed exception for details.";
                throw new InvalidOperationException(error, ex);
            }
        }

        void OnPeekCompleted(object sender, PeekCompletedEventArgs peekCompletedEventArgs)
        {
            stopResetEvent.Reset();

            CallPeekWithExceptionHandling((queue, result) => queue.EndPeek(result), receiveQueue, circuitBreaker, criticalError, peekCompletedEventArgs.AsyncResult);

            throttlingSemaphore.Wait();

            Task.Run(() => Action(transactionSettings, transactionOptions, unitOfWork, receiveQueue, errorQueue, circuitBreaker, criticalError, peekResetEvent, receiveTimeout, throttlingSemaphore, tryProcessMessage, endProcessMessage))
                .ContinueWith(task => task.Exception.Handle(ex =>
                {
                    Logger.Error("Error processing message.", ex);
                    return true;
                }), TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

            //We using an AutoResetEvent here to make sure we do not call another BeginPeek before the Receive has been called
            peekResetEvent.WaitOne();

            CallPeekWithExceptionHandling((queue, _) => queue.BeginPeek(), receiveQueue, circuitBreaker, criticalError, peekCompletedEventArgs.AsyncResult);

            stopResetEvent.Set();
        }

        static void Action(TransactionSettings transactionSettings, TransactionOptions transactionOptions, MsmqUnitOfWork unitOfWork, MessageQueue receiveQueue, MessageQueue errorQueue, CircuitBreaker circuitBreaker, CriticalError criticalError, AutoResetEvent peekResetEvent, TimeSpan receiveTimeout, SemaphoreSlim throttlingSemaphore, Func<TransportMessage, bool> tryProcessMessage,
            Action<TransportMessage, Exception> endProcessMessage)
        {
            TransportMessage transportMessage = null;
            try
            {
                if (transactionSettings.IsTransactional)
                {
                    if (transactionSettings.SuppressDistributedTransactions)
                    {
                        using (var transaction = new MessageQueueTransaction())
                        {
                            Message message;

                            if (!TryReceiveMessage((queue, timeout, tx) =>
                            {
                                tx.Begin();
                                return queue.Receive(timeout, tx);
                            }, receiveQueue, circuitBreaker, criticalError, peekResetEvent, receiveTimeout, out message, transaction))
                            {
                                return;
                            }

                            try
                            {
                                unitOfWork.SetTransaction(transaction);

                                try
                                {
                                    transportMessage = NServiceBus.MsmqUtilities.Convert(message);
                                }
                                catch (Exception exception)
                                {
                                    LogCorruptedMessage(message.Id, errorQueue.QueueName, exception);
                                    errorQueue.Send(message, transaction);
                                    transaction.Commit();
                                    return;
                                }

                                if (tryProcessMessage(transportMessage))
                                {
                                    transaction.Commit();
                                }
                                else
                                {
                                    transaction.Abort();
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

                            if (!TryReceiveMessage((queue, timeout, _) => queue.Receive(timeout, MessageQueueTransactionType.Automatic), receiveQueue, circuitBreaker, criticalError, peekResetEvent, receiveTimeout, out message))
                            {
                                scope.Complete();
                                return;
                            }

                            try
                            {
                                transportMessage = NServiceBus.MsmqUtilities.Convert(message);
                            }
                            catch (Exception exception)
                            {
                                LogCorruptedMessage(message.Id, errorQueue.QueueName, exception);
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

                    if (!TryReceiveMessage((queue, timeout, _) => queue.Receive(timeout, MessageQueueTransactionType.None), receiveQueue, circuitBreaker, criticalError, peekResetEvent, receiveTimeout, out message))
                    {
                        return;
                    }

                    try
                    {
                        transportMessage = NServiceBus.MsmqUtilities.Convert(message);
                    }
                    catch (Exception exception)
                    {
                        LogCorruptedMessage(message.Id, errorQueue.QueueName, exception);
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

        static void LogCorruptedMessage(string messageId, string queueName, Exception ex)
        {
            var error = $"Message '{messageId}' is corrupt and will be moved to '{queueName}'";
            Logger.Error(error, ex);
        }

        static void CallPeekWithExceptionHandling(Action<MessageQueue, IAsyncResult> action, MessageQueue queue, CircuitBreaker circuitBreaker, CriticalError criticalError, IAsyncResult asyncResult = null)
        {
            try
            {
                action(queue, asyncResult);
            }
            catch (MessageQueueException messageQueueException)
            {
                RaiseCriticalException(queue.FormatName, circuitBreaker, criticalError, messageQueueException);
            }
        }

        [DebuggerNonUserCode]
        static bool TryReceiveMessage(Func<MessageQueue, TimeSpan, MessageQueueTransaction, Message> receive, MessageQueue queue, CircuitBreaker circuitBreaker, CriticalError criticalError, AutoResetEvent peekResetEvent, TimeSpan receiveTimeout, out Message message, MessageQueueTransaction transaction = null)
        {
            message = null;

            try
            {
                message = receive(queue, receiveTimeout, transaction);
                return true;
            }
            catch (MessageQueueException messageQueueException)
            {
                if (messageQueueException.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    //We should only get an IOTimeout exception here if another process removed the message between us peeking and now.
                    return false;
                }

                RaiseCriticalException(queue.FormatName, circuitBreaker, criticalError, messageQueueException);
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

        static void RaiseCriticalException(string formatName, CircuitBreaker circuitBreaker, CriticalError criticalError, MessageQueueException messageQueueException)
        {
            var errorException = $"Failed to peek messages from [{formatName}].";

            if (messageQueueException.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
            {
                errorException =
                    $"Do not have permission to access queue [{formatName}]. Make sure that the current user [{GetUserName()}] has permission to Send, Receive, and Peek  from this receiveQueue.";
            }

            circuitBreaker.Execute(() => criticalError.Raise("Error in receiving messages.", new InvalidOperationException(errorException, messageQueueException)));
        }

        static string GetUserName()
        {
            var windowsIdentity = WindowsIdentity.GetCurrent();
            return windowsIdentity.Name;
        }

        CircuitBreaker circuitBreaker = new CircuitBreaker(100, TimeSpan.FromSeconds(30));
        Configure configure;
        CriticalError criticalError;
        Action<TransportMessage, Exception> endProcessMessage;
        MessageQueue errorQueue;
        int maximumConcurrencyLevel;
        AutoResetEvent peekResetEvent = new AutoResetEvent(false);
        MessageQueue receiveQueue;
        TimeSpan receiveTimeout = TimeSpan.FromSeconds(1);
        ManualResetEvent stopResetEvent = new ManualResetEvent(true);
        SemaphoreSlim throttlingSemaphore;
        TransactionOptions transactionOptions;
        TransactionSettings transactionSettings;
        Func<TransportMessage, bool> tryProcessMessage;

        [SkipWeaving] MsmqUnitOfWork unitOfWork;
    }
}