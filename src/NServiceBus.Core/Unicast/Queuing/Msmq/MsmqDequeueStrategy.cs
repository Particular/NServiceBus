namespace NServiceBus.Unicast.Queuing.Msmq
{
    using System;
    using System.Messaging;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Schedulers;
    using Logging;
    using NServiceBus.Config;
    using Transport.Transactional;
    using Utils;

    /// <summary>
    ///     Default implementation of the MSMQ <see cref="IDequeueMessages" />.
    /// </summary>
    public class MsmqDequeueStrategy : IDequeueMessages
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (MsmqDequeueStrategy));
        private readonly TimeSpan receiveTimeout = TimeSpan.FromSeconds(1);
        private int numberOfExceptionsThrown;
        private MessageQueue queue;
        private TaskScheduler scheduler;
        private SemaphoreSlim semaphore;
        private Timer timer;
        private TransactionSettings transactionSettings;

        /// <summary>
        ///     Purges the queue on startup.
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        /// <summary>
        ///     Initialises the <see cref="IDequeueMessages" />.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="settings">
        ///     The <see cref="TransactionSettings" /> to be used by <see cref="IDequeueMessages" />.
        /// </param>
        public void Init(Address address, TransactionSettings settings)
        {
            transactionSettings = settings;

            if (address == null)
            {
                throw new ArgumentException("Input queue must be specified");
            }

            if (!address.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    string.Format("Input queue [{0}] must be on the same machine as this process [{1}].",
                                  address, Environment.MachineName));
            }

            queue = new MessageQueue(MsmqUtilities.GetFullPath(address), false, true, QueueAccessMode.Receive);

            if (transactionSettings.IsTransactional && !QueueIsTransactional())
            {
                throw new ArgumentException("Queue must be transactional (" + address + ").");
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
        ///     Starts the dequeuing of message using the specified <paramref name="maxDegreeOfParallelism" />.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">The max degree of parallelism supported.</param>
        public void Start(int maxDegreeOfParallelism)
        {
            scheduler = new IOCompletionPortTaskScheduler(maxDegreeOfParallelism, maxDegreeOfParallelism);
            semaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);

            timer = new Timer(state => numberOfExceptionsThrown = 0, null, TimeSpan.FromSeconds(30),
                              TimeSpan.FromSeconds(30));

            queue.PeekCompleted += OnPeekCompleted;

            CallPeekWithExceptionHandling(() => queue.BeginPeek());
        }

        /// <summary>
        ///     updates the max degree of parallelism supported.
        /// </summary>
        /// <param name="value">The new max degree of parallelism supported.</param>
        public void ChangeMaxDegreeOfParallelism(int value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Stops the dequeuing of messages.
        /// </summary>
        public void Stop()
        {
            timer.Dispose();
            queue.PeekCompleted -= OnPeekCompleted;

            semaphore.Dispose();

            var disposableScheduler = scheduler as IDisposable;
            if (disposableScheduler != null)
            {
                disposableScheduler.Dispose();
            }
        }

        /// <summary>
        ///     Fires when a message has been dequeued.
        /// </summary>
        public event EventHandler<TransportMessageAvailableEventArgs> MessageDequeued;

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

        private MessageQueueTransactionType GetTransactionTypeForReceive()
        {
            if (!transactionSettings.IsTransactional)
                return MessageQueueTransactionType.None;

            if (Endpoint.DontUseDistributedTransactions)
                return MessageQueueTransactionType.Single;

            return MessageQueueTransactionType.Automatic;
        }

        private void OnPeekCompleted(object sender, PeekCompletedEventArgs peekCompletedEventArgs)
        {
            CallPeekWithExceptionHandling(() => queue.EndPeek(peekCompletedEventArgs.AsyncResult));

            semaphore.Wait();

            Task.Factory.StartNew(() =>
                {
                    if (transactionSettings.IsTransactional)
                    {
                        new TransactionWrapper().RunInTransaction(FireMessageDequeueEvent,
                                                                  transactionSettings.IsolationLevel,
                                                                  transactionSettings.TransactionTimeout);
                    }
                    else
                    {
                        FireMessageDequeueEvent();
                    }
                }, CancellationToken.None, TaskCreationOptions.None, scheduler)
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Logger.Error("Error processing message.", task.Exception);
                    }
                })
                .ContinueWith(task => semaphore.Release())
                .ContinueWith(task =>
                    {
                        AggregateException ignore = task.Exception;
                    });

            CallPeekWithExceptionHandling(() => queue.BeginPeek());
        }

        private TransportMessage Receive()
        {
            try
            {
                using (Message m = queue.Receive(receiveTimeout, GetTransactionTypeForReceive()))
                {
                    if (m == null)
                    {
                        return null;
                    }

                    return MsmqUtilities.Convert(m);
                }
            }
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    return null;
                }

                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
                {
                    WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();

                    string errorException =
                        string.Format(
                            "Do not have permission to access queue [{0}]. Make sure that the current user [{1}] has permission to Send, Receive, and Peek  from this queue.",
                            queue.FormatName,
                            windowsIdentity != null
                                ? windowsIdentity.Name
                                : "Unknown User");
                    OnCriticalExceptionEncountered(new InvalidOperationException(errorException, mqe));

                    return null;
                }

                if (Interlocked.Increment(ref numberOfExceptionsThrown) > 100)
                {
                    OnCriticalExceptionEncountered(new InvalidOperationException
                                                       (
                                                       string.Format(
                                                           "Failed to receive messages from [{0}].",
                                                           queue.FormatName),
                                                       mqe));
                    return null;
                }

                throw;
            }
        }

        private void CallPeekWithExceptionHandling(Action action)
        {
            try
            {
                action();
            }
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    return;
                }

                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
                {
                    WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();

                    string errorException =
                        string.Format(
                            "Do not have permission to access queue [{0}]. Make sure that the current user [{1}] has permission to Send, Receive, and Peek  from this queue.",
                            queue.FormatName,
                            windowsIdentity != null
                                ? windowsIdentity.Name
                                : "Unknown User");
                    OnCriticalExceptionEncountered(new InvalidOperationException(errorException, mqe));

                    return;
                }

                if (Interlocked.Increment(ref numberOfExceptionsThrown) > 100)
                {
                    OnCriticalExceptionEncountered(new InvalidOperationException(
                                                       string.Format(
                                                           "Failed to peek messages from [{0}].",
                                                           queue.FormatName),
                                                       mqe));
                    return;
                }

                Logger.Error("Error in peeking message.", mqe);
            }
        }

        private void FireMessageDequeueEvent()
        {
            TransportMessage message = null;

            try
            {
                message = Receive();
            }
            catch (Exception ex)
            {
                Logger.Error("Error in receiving messages.", ex);
            }

            if (message != null)
            {
                MessageDequeued(this, new TransportMessageAvailableEventArgs(message));
            }
        }

        private static void OnCriticalExceptionEncountered(Exception ex)
        {
            Logger.Fatal("Error in receiving messages.", ex);

            Configure.Instance.OnCriticalError(String.Format("Error in receiving messages.\n{0}", ex));
        }
    }
}