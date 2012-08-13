using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Transactions;
using NServiceBus.Logging;
using NServiceBus.Faults;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Utils;

namespace NServiceBus.Unicast.Transport.Transactional
{
    public class TransactionalTransport : ITransport
    {
        #region config info

        /// <summary>
        /// Sets whether or not the transport is transactional.
        /// </summary>
        public bool IsTransactional { get; set; }

        private int maxRetries = 5;

        /// <summary>
        /// Sets the maximum number of times a message will be retried
        /// when an exception is thrown as a result of handling the message.
        /// This value is only relevant when <see cref="IsTransactional"/> is true.
        /// </summary>
        /// <remarks>
        /// Default value is 5.
        /// </remarks>
        public int MaxRetries
        {
            get { return maxRetries; }
            set { maxRetries = value; }
        }

        /// <summary>
        /// Property for getting/setting the period of time when the transaction times out.
        /// Only relevant when <see cref="IsTransactional"/> is set to true.
        /// </summary>
        public TimeSpan TransactionTimeout { get; set; }

        /// <summary>
        /// Property for getting/setting the isolation level of the transaction scope.
        /// Only relevant when <see cref="IsTransactional"/> is set to true.
        /// </summary>
        public IsolationLevel IsolationLevel { get; set; }

        /// <summary>
        /// Sets the object which will be used for receiving messages.
        /// </summary>
        public IReceiveMessages MessageReceiver { get; set; }

        /// <summary>
        /// Manages failed message processing.
        /// </summary>
        public IManageMessageFailures FailureManager { get; set; }

        #endregion

        #region ITransport Members

        /// <summary>
        /// Event which indicates that message processing has started.
        /// </summary>
        public event EventHandler<StartedMessageProcessingEventArgs> StartedMessageProcessing;

        /// <summary>
        /// Event which indicates that message processing has completed.
        /// </summary>
        public event EventHandler FinishedMessageProcessing;

        /// <summary>
        /// Event which indicates that message processing failed for some reason.
        /// </summary>
        public event EventHandler<FailedMessageProcessingEventArgs> FailedMessageProcessing;

        /// <summary>
        /// Gets/sets the number of concurrent threads that should be
        /// created for processing the queue.
        /// 
        /// Get returns the actual number of running worker threads, which may
        /// be different than the originally configured value.
        /// 
        /// When used as a setter, this value will be used by the <see cref="Start"/>
        /// method only and will have no effect if called afterwards.
        /// 
        /// To change the number of worker threads at runtime, call <see cref="ChangeNumberOfWorkerThreads"/>.
        /// </summary>
        public virtual int NumberOfWorkerThreads
        {
            get
            {
                lock (workerThreads)
                    return workerThreads.Count;
            }
            set
            {
                numberOfWorkerThreads = value;
            }
        }

        private int throttlingMilliseconds;
        private int maxThroughputPerSecond;
        private const int AverageMessageHandlingTime = 200; // "guessed" time it takes for user code in handler to execute.
        /// <summary>
        /// Throttling receiving messages rate. You can't set the value other than the value specified at your license.
        /// </summary>
        public int MaxThroughputPerSecond {
            get { return maxThroughputPerSecond; }
            set
            {
                maxThroughputPerSecond = value;
                if (maxThroughputPerSecond == 0)
                {
                    throttlingMilliseconds = 0;
                    Logger.Debug("Throttling on message receiving rate is not limited by licensing policy.");
                    return;
                }

                if(maxThroughputPerSecond > 0)
                    throttlingMilliseconds = (1000 - AverageMessageHandlingTime) / maxThroughputPerSecond;
                Logger.DebugFormat("Setting throttling to: [{0}] message/s per second, sleep between receiving message: [{1}]", maxThroughputPerSecond, throttlingMilliseconds);
            }
        }

        /// <summary>
        /// If set to true the transaction scope will be suppressed to avoid the use of DTC
        /// </summary>
        public bool SuppressDTC { get; set; }

        private int numberOfWorkerThreads = 1;


        /// <summary>
        /// Event raised when a message has been received in the input queue.
        /// </summary>
        public event EventHandler<TransportMessageReceivedEventArgs> TransportMessageReceived;

        /// <summary>
        /// Changes the number of worker threads to the given target,
        /// stopping or starting worker threads as needed.
        /// </summary>
        /// <param name="targetNumberOfWorkerThreads"></param>
        public void ChangeNumberOfWorkerThreads(int targetNumberOfWorkerThreads)
        {
            lock (workerThreads)
            {
                var current = workerThreads.Count;

                if (targetNumberOfWorkerThreads == current)
                    return;

                if (targetNumberOfWorkerThreads < current)
                {
                    for (var i = targetNumberOfWorkerThreads; i < current; i++)
                        workerThreads[i].Stop();

                    return;
                }

                if (targetNumberOfWorkerThreads > current)
                {
                    for (var i = current; i < targetNumberOfWorkerThreads; i++)
                        AddWorkerThread().Start();

                    return;
                }
            }
        }

        void ITransport.Start(string inputqueue)
        {
            ((ITransport)this).Start(Address.Parse(inputqueue));
        }

        void ITransport.Start(Address address)
        {
            MessageReceiver.Init(address, IsTransactional);
            FailureManager.Init(address);
            Logger.DebugFormat("Going to start [{0}] receiving thread/s for Address [{1}].", numberOfWorkerThreads, address);
            for (int i = 0; i < numberOfWorkerThreads; i++)
                AddWorkerThread().Start();
        }

        #endregion

        #region helper methods

        private WorkerThread AddWorkerThread()
        {
            lock (workerThreads)
            {
                var result = new WorkerThread(Process);

                workerThreads.Add(result);

                result.Stopped += delegate(object sender, EventArgs e)
                                      {
                                          var wt = sender as WorkerThread;
                                          lock (workerThreads)
                                              workerThreads.Remove(wt);
                                      };

                return result;
            }
        }

        /// <summary>
        /// Waits for a message to become available on the input queue
        /// and then receives it.
        /// </summary>
        /// <remarks>
        /// If the queue is transactional the receive operation will be wrapped in a 
        /// transaction.
        /// </remarks>
        private void Process()
        {
            if (throttlingMilliseconds > 0)
                Thread.Sleep(throttlingMilliseconds);

            _needToAbort = false;
            _messageId = string.Empty;

            try
            {
                if (IsTransactional)
                    new TransactionWrapper().RunInTransaction(ReceiveMessage, IsolationLevel, TransactionTimeout);
                else
                    ReceiveMessage();

                ClearFailuresForMessage(_messageId);
            }
            catch (AbortHandlingCurrentMessageException)
            {
                //in case AbortHandlingCurrentMessage was called
                return; //don't increment failures, we want this message kept around.
            }
            catch (Exception e)
            {
                var originalException = e;

                if (e is TransportMessageHandlingFailedException)
                    originalException = ((TransportMessageHandlingFailedException)e).OriginalException;

                if (IsTransactional)
                {
                    IncrementFailuresForMessage(_messageId, originalException);
                }

                OnFailedMessageProcessing(originalException);
            }
        }

        /// <summary>
        /// Receives a message from the input queue.
        /// </summary>
        /// <remarks>
        /// If a message is received the <see cref="TransportMessageReceived"/> event will be raised.
        /// </remarks>
        public void ReceiveMessage()
        {
            var m = Receive();
            if (m == null)
                return;

            if(SuppressDTC)
            {
                using(new TransactionScope(TransactionScopeOption.Suppress))
                    ProcessMessage(m);
            }
            else
            {
                ProcessMessage(m);     
            }
        }

        void ProcessMessage(TransportMessage m)
        {
            _messageId = m.Id;

            var exceptionFromStartedMessageHandling = OnStartedMessageProcessing(m);

            if (IsTransactional)
            {
                if (HandledMaxRetries(m))
                {
                    Logger.Error(string.Format("Message has failed the maximum number of times allowed, ID={0}.", m.Id));

                    OnFinishedMessageProcessing();

                    return;
                }
            }

            if (exceptionFromStartedMessageHandling != null)
                throw exceptionFromStartedMessageHandling; //cause rollback 

            //care about failures here
            var exceptionFromMessageHandling = OnTransportMessageReceived(m);

            //and here
            var exceptionFromMessageModules = OnFinishedMessageProcessing();

            //but need to abort takes precedence - failures aren't counted here,
            //so messages aren't moved to the error queue.
            if (_needToAbort)
                throw new AbortHandlingCurrentMessageException();

            if (exceptionFromMessageHandling != null) //cause rollback
                throw exceptionFromMessageHandling;

            if (exceptionFromMessageModules != null) //cause rollback
                throw exceptionFromMessageModules;
        }

        private bool HandledMaxRetries(TransportMessage message)
        {
            string messageId = message.Id;
            failuresPerMessageLocker.EnterReadLock();

            if (failuresPerMessage.ContainsKey(messageId) &&
                (failuresPerMessage[messageId] >= maxRetries))
            {
                failuresPerMessageLocker.ExitReadLock();
                failuresPerMessageLocker.EnterWriteLock();

                var ex = exceptionsForMessages[messageId];
                InvokeFaultManager(message, ex);
                failuresPerMessage.Remove(messageId);
                exceptionsForMessages.Remove(messageId);

                failuresPerMessageLocker.ExitWriteLock();

                return true;
            }

            failuresPerMessageLocker.ExitReadLock();
            return false;
        }

        void InvokeFaultManager(TransportMessage message, Exception exception)
        {
            try
            {
                FailureManager.ProcessingAlwaysFailsForMessage(message, exception);
            }
            catch (Exception ex)
            {
                Logger.FatalFormat("Fault manager failed to process the failed message {0}", ex, message);
                Configure.Instance.OnCriticalError();
            }

        }

        private void ClearFailuresForMessage(string messageId)
        {
            failuresPerMessageLocker.EnterReadLock();
            if (failuresPerMessage.ContainsKey(messageId))
            {
                failuresPerMessageLocker.ExitReadLock();
                failuresPerMessageLocker.EnterWriteLock();

                failuresPerMessage.Remove(messageId);
                exceptionsForMessages.Remove(messageId);

                failuresPerMessageLocker.ExitWriteLock();
            }
            else
                failuresPerMessageLocker.ExitReadLock();
        }

        private void IncrementFailuresForMessage(string messageId, Exception e)
        {
            try
            {
                failuresPerMessageLocker.EnterWriteLock();

                if (!failuresPerMessage.ContainsKey(messageId))
                    failuresPerMessage[messageId] = 1;
                else
                    failuresPerMessage[messageId] = failuresPerMessage[messageId] + 1;

                exceptionsForMessages[messageId] = e;
            }
            catch { } //intentionally swallow exceptions here
            finally
            {
                failuresPerMessageLocker.ExitWriteLock();
            }
        }

        [DebuggerNonUserCode] // so that exceptions don't interfere with debugging.
        private TransportMessage Receive()
        {
            try
            {
                return MessageReceiver.Receive();
            }
            catch (InvalidOperationException)
            {
                Configure.Instance.OnCriticalError();
                return null;
            }
            catch (Exception e)
            {
                Logger.Error("Error in receiving messages.", e);
                return null;
            }
        }

        /// <summary>
        /// Causes the processing of the current message to be aborted.
        /// </summary>
        public void AbortHandlingCurrentMessage()
        {
            _needToAbort = true;
        }

        private Exception OnStartedMessageProcessing(TransportMessage msg)
        {
            try
            {
                if (StartedMessageProcessing != null)
                    StartedMessageProcessing(this, new StartedMessageProcessingEventArgs(msg));
            }
            catch (Exception e)
            {
                Logger.Error("Failed raising 'started message processing' event.", e);
                return e;
            }

            return null;
        }


        private Exception OnFinishedMessageProcessing()
        {
            try
            {
                if (FinishedMessageProcessing != null)
                    FinishedMessageProcessing(this, null);
            }
            catch (Exception e)
            {
                Logger.Error("Failed raising 'finished message processing' event.", e);
                return e;
            }

            return null;
        }

        private Exception OnTransportMessageReceived(TransportMessage msg)
        {
            try
            {
                if (TransportMessageReceived != null)
                    TransportMessageReceived(this, new TransportMessageReceivedEventArgs(msg));
            }
            catch (Exception e)
            {
                Logger.Warn("Failed raising 'transport message received' event for message with ID=" + msg.Id, e);

                return e;
            }

            return null;
        }

        private bool OnFailedMessageProcessing(Exception originalException)
        {
            try
            {
                if (FailedMessageProcessing != null)
                    FailedMessageProcessing(this, new FailedMessageProcessingEventArgs(originalException));
            }
            catch (Exception e)
            {
                Logger.Warn("Failed raising 'failed message processing' event.", e);
                return false;
            }

            return true;
        }

        #endregion

        #region members

        private readonly IList<WorkerThread> workerThreads = new List<WorkerThread>();

        private readonly ReaderWriterLockSlim failuresPerMessageLocker = new ReaderWriterLockSlim();
        /// <summary>
        /// Accessed by multiple threads - lock using failuresPerMessageLocker.
        /// </summary>
        private readonly IDictionary<string, int> failuresPerMessage = new Dictionary<string, int>();

        /// <summary>
        /// Accessed by multiple threads, manage together with failuresPerMessage.
        /// </summary>
        private readonly IDictionary<string, Exception> exceptionsForMessages = new Dictionary<string, Exception>();

        [ThreadStatic]
        private static volatile bool _needToAbort;

        [ThreadStatic]
        private static volatile string _messageId;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionalTransport));

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Stops all worker threads.
        /// </summary>
        public void Dispose()
        {
            lock (workerThreads)
                for (var i = 0; i < workerThreads.Count; i++)
                    workerThreads[i].Stop();
        }

        #endregion
    }
}
