namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Diagnostics;
    using System.Messaging;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using CircuitBreakers;
    using Logging;
    using Support;
    using Unicast.Transport;

    /// <summary>
    ///     Default implementation of <see cref="IDequeueMessages" /> for MSMQ.
    /// </summary>
    public class MsmqDequeueStrategy : IDequeueMessages
    {
        /// <summary>
        ///     Purges the queue on startup.
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        /// <summary>
        /// Msmq unit of work to be used in non DTC mode <see cref="MsmqUnitOfWork"/>.
        /// </summary>
        public MsmqUnitOfWork UnitOfWork { get; set; }

        /// <summary>
        /// Initializes the <see cref="IDequeueMessages"/>.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="settings">The <see cref="TransactionSettings"/> to be used by <see cref="IDequeueMessages"/>.</param>
        /// <param name="tryProcessMessage">Called when a message has been dequeued and is ready for processing.</param>
        /// <param name="endProcessMessage">Needs to be called by <see cref="IDequeueMessages"/> after the message has been processed regardless if the outcome was successful or not.</param>
        public void Init(Address address, TransactionSettings settings, Func<TransportMessage, bool> tryProcessMessage, Action<TransportMessage, Exception> endProcessMessage)
        {
            this.tryProcessMessage = tryProcessMessage;
            this.endProcessMessage = endProcessMessage;
            endpointAddress = address;
            transactionSettings = settings;

            if (address == null)
            {
                throw new ArgumentException("Input queue must be specified");
            }

            if (!address.Machine.Equals(RuntimeEnvironment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    string.Format("Input queue [{0}] must be on the same machine as this process [{1}].",
                                  address, RuntimeEnvironment.MachineName));
            }

            transactionOptions = new TransactionOptions { IsolationLevel = transactionSettings.IsolationLevel, Timeout = transactionSettings.TransactionTimeout };

            queue = new MessageQueue(MsmqUtilities.GetFullPath(address), false, true, QueueAccessMode.Receive);
            errorQueue = new MessageQueue(MsmqUtilities.GetFullPath(ErrorQueue), false, true, QueueAccessMode.Send);

            if (transactionSettings.IsTransactional && !QueueIsTransactional())
            {
                throw new ArgumentException("Queue must be transactional if you configure your endpoint to be transactional (" + address + ").");
            }

            var mpf = new MessagePropertyFilter();
            mpf.SetAll();

            queue.MessageReadPropertyFilter = mpf;

            if (PurgeOnStartup)
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
            semaphore = new SemaphoreSlim(maximumConcurrencyLevel, maximumConcurrencyLevel);

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

            semaphore.Dispose();
            queue.Dispose();
        }

        private bool QueueIsTransactional()
        {
            try
            {
                return queue.Transactional;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "There is a problem with the input queue: {0}. See the enclosed exception for details.",
                        queue.Path), ex);
            }
        }

        private void OnPeekCompleted(object sender, PeekCompletedEventArgs peekCompletedEventArgs)
        {
            stopResetEvent.Reset();

            CallPeekWithExceptionHandling(() => queue.EndPeek(peekCompletedEventArgs.AsyncResult));

            semaphore.Wait();

            Task.Factory
                .StartNew(Action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .ContinueWith(task =>
                    {
                        task.Exception.Handle(ex =>
                            {
                                Logger.Error("Error processing message.", ex);
                                return true;
                            });
                    }, TaskContinuationOptions.OnlyOnFaulted);

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
                    if (transactionSettings.DontUseDistributedTransactions)
                    {
                        using (var msmqTransaction = new MessageQueueTransaction())
                        {
                            msmqTransaction.Begin();

                            Message message;

                            if (!TryReceiveMessage(() => queue.Receive(receiveTimeout, msmqTransaction), out message))
                            {
                                msmqTransaction.Commit();
                                return;
                            }

                            try
                            {
                                UnitOfWork.SetTransaction(msmqTransaction);

                                try
                                {
                                    transportMessage = MsmqUtilities.Convert(message);
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
                                UnitOfWork.ClearTransaction();
                            }
                        }
                    }
                    else
                    {
                        using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
                        {
                            Message message;

                            if (!TryReceiveMessage(() => queue.Receive(receiveTimeout, MessageQueueTransactionType.Automatic), out message))
                            {
                                scope.Complete();
                                return;
                            }

                            try
                            {
                                transportMessage = MsmqUtilities.Convert(message);
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

                    if (!TryReceiveMessage(() => queue.Receive(receiveTimeout, MessageQueueTransactionType.None), out message))
                    {
                        return;
                    }

                    try
                    {
                        transportMessage = MsmqUtilities.Convert(message);
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
                semaphore.Release();
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
            catch (MessageQueueException mqe)
            {
                string errorException = string.Format("Failed to peek messages from [{0}].", queue.FormatName);

                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
                {
                    WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();

                    errorException =
                        string.Format(
                            "Do not have permission to access queue [{0}]. Make sure that the current user [{1}] has permission to Send, Receive, and Peek  from this queue.",
                            queue.FormatName,
                            windowsIdentity != null
                                ? windowsIdentity.Name
                                : "Unknown User");
                }

                RaiseCriticalException(new InvalidOperationException(errorException, mqe));
            }
        }

        [DebuggerNonUserCode]
        bool TryReceiveMessage(Func<Message> receive, out Message message)
        {
            message = null;

            try
            {
                message = receive();
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
        
        void RaiseCriticalException(Exception ex)
        {
            circuitBreaker.Execute(() => Configure.Instance.RaiseCriticalError("Error in receiving messages.", ex));
        }

        readonly CircuitBreaker circuitBreaker = new CircuitBreaker(100, TimeSpan.FromSeconds(30));
        Func<TransportMessage, bool> tryProcessMessage;
        static readonly ILog Logger = LogManager.GetLogger(typeof(MsmqDequeueStrategy));
        readonly ManualResetEvent stopResetEvent = new ManualResetEvent(true);
        readonly TimeSpan receiveTimeout = TimeSpan.FromSeconds(1);
        readonly AutoResetEvent peekResetEvent = new AutoResetEvent(false);
        MessageQueue queue;
        MessageQueue errorQueue;
        SemaphoreSlim semaphore;
        TransactionSettings transactionSettings;
        Address endpointAddress;
        TransactionOptions transactionOptions;
        Action<TransportMessage, Exception> endProcessMessage;
    }
}