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
        private readonly ManualResetEvent stopResetEvent = new ManualResetEvent(true);
        private readonly TimeSpan receiveTimeout = TimeSpan.FromSeconds(1);
        private readonly AutoResetEvent peekResetEvent = new AutoResetEvent(false);
        private int numberOfExceptionsThrown;
        private MessageQueue queue;
        private TaskScheduler scheduler;
        private SemaphoreSlim semaphore;
        private Timer timer;
        private TransactionSettings transactionSettings;
        private Address endpointAddress;

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
            endpointAddress = address;
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
        ///     Starts the dequeuing of message using the specified <paramref name="maximumConcurrencyLevel" />.
        /// </summary>
        /// <param name="maximumConcurrencyLevel">The maximum concurrency level supported.</param>
        public void Start(int maximumConcurrencyLevel)
        {
            scheduler = new MTATaskScheduler(maximumConcurrencyLevel, String.Format("NServiceBus Dequeuer Worker Thread for [{0}]", endpointAddress));
            semaphore = new SemaphoreSlim(maximumConcurrencyLevel, maximumConcurrencyLevel);

            timer = new Timer(state => numberOfExceptionsThrown = 0, null, TimeSpan.FromSeconds(30),
                              TimeSpan.FromSeconds(30));

            queue.PeekCompleted += OnPeekCompleted;

            CallPeekWithExceptionHandling(() => queue.BeginPeek());
        }

        /// <summary>
        ///     Stops the dequeuing of messages.
        /// </summary>
        public void Stop()
        {
            timer.Dispose();

            queue.PeekCompleted -= OnPeekCompleted;

            stopResetEvent.WaitOne();

            var disposableScheduler = scheduler as IDisposable;
            if (disposableScheduler != null)
            {
                disposableScheduler.Dispose();
            }

            semaphore.Dispose();
            queue.Dispose();
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
            stopResetEvent.Reset();

            CallPeekWithExceptionHandling(() => queue.EndPeek(peekCompletedEventArgs.AsyncResult));

            semaphore.Wait();

            Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (transactionSettings.IsTransactional)
                        {
                            new TransactionWrapper().RunInTransaction(ReceiveAndFireEvent,
                                                                      transactionSettings.IsolationLevel,
                                                                      transactionSettings.TransactionTimeout);
                        }
                        else
                        {
                            ReceiveAndFireEvent();
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, CancellationToken.None, TaskCreationOptions.None, scheduler)
                .ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Logger.Error("Error processing message.", task.Exception);
                        }
                    });

            //We using an AutoResetEvent here to make sure we do not call another BeginPeek before the Receive has been called
            peekResetEvent.WaitOne();

            CallPeekWithExceptionHandling(() => queue.BeginPeek());

            stopResetEvent.Set();
        }

        private void CallPeekWithExceptionHandling(Action action)
        {
            try
            {
                action();
            }
            catch (MessageQueueException mqe)
            {
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

        private void ReceiveAndFireEvent()
        {
            Message message = null;
            try
            {
                message = queue.Receive(receiveTimeout, GetTransactionTypeForReceive());
            }
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    //We should only get an IOTimeout exception here if another process removed the message between us peeking and now.
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
                }

                if (Interlocked.Increment(ref numberOfExceptionsThrown) > 100)
                {
                    OnCriticalExceptionEncountered(new InvalidOperationException
                                                       (
                                                       string.Format(
                                                           "Failed to receive messages from [{0}].",
                                                           queue.FormatName),
                                                       mqe));
                }

                Logger.Error("Error in receiving messages.", mqe);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in receiving messages.", ex);
            }
            finally
            {
                peekResetEvent.Set();
            }

            if (message == null)
            {
                return;
            }

            TransportMessage transportMessage = null;
            try
            {
                transportMessage = MsmqUtilities.Convert(message);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in converting message to TransportMessage.", ex);
            }

            if (transportMessage != null)
            {
                MessageDequeued(this, new TransportMessageAvailableEventArgs(transportMessage));
            }
        }

        private static void OnCriticalExceptionEncountered(Exception ex)
        {
            Configure.Instance.OnCriticalError("Error in receiving messages.", ex);
        }
    }
}