namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;
    using System.Security.Principal;
    using System.Threading;
    using Janitor;
    using NServiceBus.CircuitBreakers;

    class MsmqDequeueStrategy : IDequeueMessages, IDisposable
    {

        public MsmqDequeueStrategy(CriticalError criticalError, bool isTransactional, MsmqAddress errorQueueAddress)
        {
            this.criticalError = criticalError;
            this.isTransactional = isTransactional;
            this.errorQueueAddress = errorQueueAddress;
        }

        public DequeueInfo Init(DequeueSettings settings)
        {
            var msmqAddress = MsmqAddress.Parse(settings.QueueName);

            var queueName = msmqAddress.Queue;
            if (!string.Equals(msmqAddress.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                var message = string.Format("MSMQ Dequeuing can only run against the local machine. Invalid queue name '{0}'", settings.QueueName);
                throw new Exception(message);
            }

            var fullPath = MsmqUtilities.GetFullPath(queueName);
            queue = new MessageQueue(fullPath, false, true, QueueAccessMode.Receive);

            if (isTransactional && !QueueIsTransactional())
            {
                throw new ArgumentException("Queue must be transactional if you configure your endpoint to be transactional (" + settings.QueueName + ").");
            }

            queue.MessageReadPropertyFilter = DefaultReadPropertyFilter;

            if (settings.PurgeOnStartup)
            {
                queue.Purge();
            }
            return new DequeueInfo(queueName);
        }

        /// <summary>
        ///     Starts the dequeuing of message using the specified
        /// </summary>
        public void Start()
        {
            MessageQueue.ClearConnectionCache();
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
            try
            {
                stopResetEvent.Reset();

                CallPeekWithExceptionHandling(() => queue.EndPeek(peekCompletedEventArgs.AsyncResult));

                observable.OnNext(new MessageAvailable(c =>
                {
                    c.Set(queue);
                    c.Set("MsmqDequeueStrategy.PeekResetEvent",peekResetEvent);
                    c.Set("MsmqDequeueStrategy.ErrorQueue",errorQueueAddress);
                }));

                peekResetEvent.WaitOne();

                CallPeekWithExceptionHandling(() => queue.BeginPeek());
            }
            finally
            {
                stopResetEvent.Set();
            }
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

        CriticalError criticalError;
        readonly bool isTransactional;
        readonly MsmqAddress errorQueueAddress;

        [SkipWeaving]
        CircuitBreaker circuitBreaker = new CircuitBreaker(100, TimeSpan.FromSeconds(30));
        MessageQueue queue;
        ManualResetEvent stopResetEvent = new ManualResetEvent(true);
        AutoResetEvent peekResetEvent = new AutoResetEvent(false);

        Observable<MessageAvailable> observable = new Observable<MessageAvailable>();

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

        public IDisposable Subscribe(IObserver<MessageAvailable> observer)
        {
            return observable.Subscribe(observer);
        }
    }
}