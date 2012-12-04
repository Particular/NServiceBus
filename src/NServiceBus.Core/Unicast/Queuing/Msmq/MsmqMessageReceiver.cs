namespace NServiceBus.Unicast.Queuing.Msmq
{
    using System;
    using System.Messaging;
    using System.Security.Principal;
    using System.Threading;
    using Logging;
    using NServiceBus.Config;

    public class CriticalExceptionEncounteredEventArgs : EventArgs
    {
        private readonly Exception exception;

        public CriticalExceptionEncounteredEventArgs(Exception m)
        {
            exception = m;
        }

        public Exception Exception
        {
            get { return exception; }
        }
    }

    public class MessageIsAvailableEventArgs : EventArgs
    {
        private readonly Lazy<TransportMessage> transportMessage;

        public MessageIsAvailableEventArgs(Func<TransportMessage> receive)
        {
            transportMessage = new Lazy<TransportMessage>(receive, false);
        }

        public TransportMessage Message
        {
            get { return transportMessage.Value; }
        }
    }

    public class MsmqMessageReceiver
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (MsmqMessageReceiver));
        private readonly TimeSpan receiveTimeout = TimeSpan.FromSeconds(1);
        private bool isTransactional;
        private int numberOfExceptionsThrown;
        private MessageQueue queue;
        private Timer timer;

        /// <summary>
        ///     Sets whether or not the transport should purge the input
        ///     queue when it is started.
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        public event EventHandler<MessageIsAvailableEventArgs> MessageIsAvailable;
        public event EventHandler<CriticalExceptionEncounteredEventArgs> CriticalExceptionEncountered;

        public void Init(Address address, bool useTransactions)
        {
            isTransactional = useTransactions;

            if (address == null)
                throw new ArgumentException("Input queue must be specified");

            string machine = address.Machine;

            if (machine.ToLower() != Environment.MachineName.ToLower())
                throw new InvalidOperationException(
                    string.Format("Input queue [{0}] must be on the same machine as this process [{1}].",
                                  address, Environment.MachineName.ToLower()));

            queue = new MessageQueue(MsmqUtilities.GetFullPath(address), false, true, QueueAccessMode.Receive);

            if (useTransactions && !QueueIsTransactional())
                throw new ArgumentException("Queue must be transactional (" + address + ").");

            var mpf = new MessagePropertyFilter();
            mpf.SetAll();

            queue.MessageReadPropertyFilter = mpf;

            if (PurgeOnStartup)
                queue.Purge();
        }

        public void Start()
        {
            timer = new Timer(state => numberOfExceptionsThrown = 0, null, TimeSpan.FromSeconds(30),
                              TimeSpan.FromSeconds(30));

            queue.PeekCompleted += OnPeekCompleted;
            CallPeekWithExceptionHandling(() => queue.BeginPeek());
        }

        public void Stop()
        {
            timer.Dispose();
            queue.PeekCompleted -= OnPeekCompleted;
        }

        private void OnPeekCompleted(object sender, PeekCompletedEventArgs peekCompletedEventArgs)
        {
            CallPeekWithExceptionHandling(() => queue.EndPeek(peekCompletedEventArgs.AsyncResult));

            MessageIsAvailable(this, new MessageIsAvailableEventArgs(Receive));

            CallPeekWithExceptionHandling(() => queue.BeginPeek());
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
                    CriticalExceptionEncountered(this,
                                                 new CriticalExceptionEncounteredEventArgs(
                                                     new InvalidOperationException(errorException, mqe)));

                    return;
                }

                if (Interlocked.Increment(ref numberOfExceptionsThrown) > 100)
                {
                    CriticalExceptionEncountered(this,
                                                 new CriticalExceptionEncounteredEventArgs(new InvalidOperationException
                                                                                               (
                                                                                               string.Format(
                                                                                                   "Failed to receive messages from [{0}].",
                                                                                                   queue.FormatName),
                                                                                               mqe)));
                    return;
                }

                Logger.Error("Error in receiving messages.", mqe);
            }
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
                    CriticalExceptionEncountered(this,
                                                 new CriticalExceptionEncounteredEventArgs(
                                                     new InvalidOperationException(errorException, mqe)));

                    return null;
                }

                if (Interlocked.Increment(ref numberOfExceptionsThrown) > 100)
                {
                    CriticalExceptionEncountered(this,
                                                 new CriticalExceptionEncounteredEventArgs(new InvalidOperationException
                                                                                               (
                                                                                               string.Format(
                                                                                                   "Failed to receive messages from [{0}].",
                                                                                                   queue.FormatName),
                                                                                               mqe)));
                    return null;
                }

                Logger.Error("Error in receiving messages.", mqe);
                return null;
            }
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

        private MessageQueueTransactionType GetTransactionTypeForReceive()
        {
            if (!isTransactional)
                return MessageQueueTransactionType.None;

            if (Endpoint.DontUseDistributedTransactions)
                return MessageQueueTransactionType.Single;

            return MessageQueueTransactionType.Automatic;
        }
    }
}