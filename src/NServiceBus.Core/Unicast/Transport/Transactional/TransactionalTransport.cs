namespace NServiceBus.Unicast.Transport.Transactional
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Transactions;
    using Faults;
    using Logging;
    using System.Linq;
    using System.Runtime.Serialization;
    using Monitoring;

    public class TransactionalTransport : ITransport
    {
        /// <summary>
        /// The receiver responsible for notifying the transport when new messages are available
        /// </summary>
        public IDequeueMessages Receiver { get; set; }

        /// <summary>
        /// Setings related to the transactionallity of the transport
        /// </summary>
        public TransactionSettings TransactionSettings
        {
            get
            {
                if (transactionSettings == null)
                    transactionSettings = new TransactionSettings();

                return transactionSettings;
            }
            set { transactionSettings = value; }
        }
        TransactionSettings transactionSettings;

        /// <summary>
        /// Manages failed message processing.
        /// </summary>
        public IManageMessageFailures FailureManager { get; set; }


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
                return maxDegreeOfParallelism;
            }
            set
            {
                if (isStarted)
                    throw new InvalidOperationException("Can't set the number of worker threads after the transport has been started. Use ChangeNumberOfWorkerThreads instead");

                maxDegreeOfParallelism = value;
            }
        }

        int maxDegreeOfParallelism;


        /// <summary>
        /// Throttling receiving messages rate. You can't set the value other than the value specified at your license.
        /// </summary>
        public int MaxThroughputPerSecond
        {
            get { return maxThroughputPerSecond; }
            set
            {
                if (isStarted)
                    throw new InvalidOperationException("Changing the throughput at runtime isn't currently supported");

                maxThroughputPerSecond = value;
            }
        }

        int maxThroughputPerSecond;

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
            maxDegreeOfParallelism = targetNumberOfWorkerThreads;

            if (isStarted)
                Receiver.ChangeMaxDegreeOfParallelism(targetNumberOfWorkerThreads);
        }

        public void Start(string inputqueue)
        {
            ((ITransport)this).Start(Address.Parse(inputqueue));
        }

        public void Start(Address address)
        {
            if (isStarted)
                throw new InvalidOperationException("The transport is already started");

            receiveAddress = address;

            FailureManager.Init(address);

            InitializePerformanceCounters();

            throughputLimiter = new ThroughputLimiter();

            throughputLimiter.Start(maxThroughputPerSecond);

            StartReceiver();

            if(maxThroughputPerSecond > 0)
                Logger.InfoFormat("Transport: {0} started with its throughpit limited to {1} messages/second",receiveAddress,maxThroughputPerSecond);

            isStarted = true;
        }

        void InitializePerformanceCounters()
        {
            currentThroughputPerformanceCounter = new ThroughputPerformanceCounter(receiveAddress);

            currentThroughputPerformanceCounter.Initialize();
        }

        ThroughputPerformanceCounter currentThroughputPerformanceCounter;

        void StartReceiver()
        {
            Receiver.Init(receiveAddress, TransactionSettings);
            Receiver.MessageDequeued += Process;

            Receiver.Start(maxDegreeOfParallelism);
        }

        void Process(object sender, TransportMessageAvailableEventArgs e)
        {
            var message = e.Message;
            _needToAbort = false;

            try
            {
                if (TransactionSettings.SuppressDTC)
                {
                    using (new TransactionScope(TransactionScopeOption.Suppress))
                        ProcessMessage(message);
                }
                else
                {
                    ProcessMessage(message);
                }

                ClearFailuresForMessage(message.Id);

                throughputLimiter.MessageProcessed();
                currentThroughputPerformanceCounter.MessageProcessed();
            }
            catch (AbortHandlingCurrentMessageException)
            {
                //in case AbortHandlingCurrentMessage was called
                //don't increment failures, we want this message kept around.
            }
            catch (Exception ex)
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    if (TransactionSettings.IsTransactional)
                    {
                        IncrementFailuresForMessage(message.Id, ex);
                    }

                    OnFailedMessageProcessing(ex);
                }

                //rethrow to cause the message to go back to the queue
                throw;
            }
        }

        #endregion

        #region helper methods


        void ProcessMessage(TransportMessage m)
        {
            var exceptionFromStartedMessageHandling = OnStartedMessageProcessing(m);

            if (TransactionSettings.IsTransactional)
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

            if (exceptionFromMessageHandling != null)
            {
                if (exceptionFromMessageHandling is AggregateException)
                {
                    var aggregateException = (AggregateException)exceptionFromMessageHandling;
                    var serializationException = aggregateException.InnerExceptions.FirstOrDefault(ex => ex.GetType() == typeof(SerializationException));
                    if (serializationException != null)
                    {
                        Logger.Error("Failed to serialize message with ID: " + m.IdForCorrelation, serializationException);
                        FailureManager.SerializationFailedForMessage(m, serializationException);
                    }
                    else
                    {
                        throw exceptionFromMessageHandling;//cause rollback    
                    }
                }
                else
                {
                    throw exceptionFromMessageHandling;//cause rollback    
                }

            }

            if (exceptionFromMessageModules != null) //cause rollback
                throw exceptionFromMessageModules;
        }

        private bool HandledMaxRetries(TransportMessage message)
        {
            string messageId = message.Id;
            failuresPerMessageLocker.EnterReadLock();

            if (failuresPerMessage.ContainsKey(messageId) &&
                (failuresPerMessage[messageId] >= TransactionSettings.MaxRetries))
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
                Logger.Fatal(string.Format("Fault manager failed to process the failed message with id {0}", message), ex);
                Configure.Instance.OnCriticalError(string.Format("Fault manager failed to process the failed message.\n{0}", ex));
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


        Address receiveAddress;
        bool isStarted;
        ThroughputLimiter throughputLimiter;

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


        static readonly ILog Logger = LogManager.GetLogger("Transport");



        /// <summary>
        /// Stops all worker threads.
        /// </summary>
        public void Dispose()
        {
            if (!isStarted)
                return;

            Receiver.Stop();
            Receiver.MessageDequeued -= Process;
            isStarted = false;
        }

    }
}
