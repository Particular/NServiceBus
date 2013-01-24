namespace NServiceBus.Unicast.Transport.Transactional
{
    using System;
    using System.Diagnostics;
    using System.Transactions;
    using Faults;
    using Logging;
    using System.Runtime.Serialization;
    using Management.Retries;
    using Monitoring;
    using Queuing;

    /// <summary>
    /// An implementation of <see cref="ITransport"/> that supports transactions.
    /// </summary>
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
        /// When used as a setter, this value will be used by the <see cref="Start(Address)"/>
        /// method only and will have no effect if called afterwards.
        /// 
        /// To change the number of worker threads at runtime, call <see cref="ChangeNumberOfWorkerThreads"/>.
        /// </summary>
        public virtual int NumberOfWorkerThreads
        {
            get { return MaximumConcurrencyLevel; }
        }

        /// <summary>
        /// Gets the maximum concurrency level this <see cref="ITransport"/> is able to support.
        /// </summary>
        public virtual int MaximumConcurrencyLevel
        {
            get { return maximumConcurrencyLevel; }
            set
            {
                if (isStarted)
                {
                    throw new InvalidOperationException(
                        "Can't set the number of worker threads after the transport has been started. Use ChangeMaximumConcurrencyLevel(int maximumConcurrencyLevel) instead.");
                }

                maximumConcurrencyLevel = value;
            }
        }

        int maximumConcurrencyLevel;

        /// <summary>
        /// Updates the maximum concurrency level this <see cref="ITransport"/> is able to support.
        /// </summary>
        /// <param name="maximumConcurrencyLevel">The new maximum concurrency level for this <see cref="ITransport"/>.</param>
        public void ChangeMaximumConcurrencyLevel(int maximumConcurrencyLevel)
        {
            if (this.maximumConcurrencyLevel == maximumConcurrencyLevel)
            {
                return;
            }

            this.maximumConcurrencyLevel = maximumConcurrencyLevel;

            if (isStarted)
            {
                Receiver.Stop();
                Receiver.Start(maximumConcurrencyLevel);
                Logger.InfoFormat("Maximum concurrency level for '{0}' changed to {1}.", receiveAddress, maximumConcurrencyLevel);
            }
        }

        /// <summary>
        /// Gets the receiving messages rate.
        /// </summary>
        public int MaximumMessageThroughputPerSecond
        {
            get { return maxMessageThroughputPerSecond; }
        }

        /// <summary>
        /// Throttling receiving messages rate. You can't set the value other than the value specified at your license.
        /// </summary>
        public int MaxThroughputPerSecond
        {
            get { return maxMessageThroughputPerSecond; }
            set
            {
                if (isStarted)
                    throw new InvalidOperationException("Can't set the maximum throughput per second after the transport has been started. Use ChangeMaximumMessageThroughputPerSecond(int maximumMessageThroughputPerSecond) instead.");

                maxMessageThroughputPerSecond = value;
            }
        }

        int maxMessageThroughputPerSecond;

        public void ChangeMaximumMessageThroughputPerSecond(int maximumMessageThroughputPerSecond)
        {
            if (maximumMessageThroughputPerSecond == maxMessageThroughputPerSecond)
            {
                return;
            }

            maxMessageThroughputPerSecond = maximumMessageThroughputPerSecond;
            if (throughputLimiter != null)
            {
                throughputLimiter.Stop();
                throughputLimiter.Start(maximumMessageThroughputPerSecond);
            }
            if (maximumMessageThroughputPerSecond <= 0)
            {
                Logger.InfoFormat("Throughput limit for {0} disabled.", receiveAddress);
            }
            else
            {
                Logger.InfoFormat("Throughput limit for {0} changed to {1} msg/sec", receiveAddress,
                                  maximumMessageThroughputPerSecond);
            }
        }

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
            ChangeMaximumConcurrencyLevel(targetNumberOfWorkerThreads);
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

            firstLevelRetries = new FirstLevelRetries(TransactionSettings.MaxRetries, FailureManager);

            InitializePerformanceCounters();

            throughputLimiter = new ThroughputLimiter();

            throughputLimiter.Start(maxMessageThroughputPerSecond);

            StartReceiver();

            if(maxMessageThroughputPerSecond > 0)
                Logger.InfoFormat("Transport: {0} started with its throughput limited to {1} msg/sec", receiveAddress, maxMessageThroughputPerSecond);

            isStarted = true;
        }

        void InitializePerformanceCounters()
        {
            currentReceivePerformanceDiagnostics = new ReceivePerformanceDiagnostics(receiveAddress);

            currentReceivePerformanceDiagnostics.Initialize();
        }

        ReceivePerformanceDiagnostics currentReceivePerformanceDiagnostics;

        void StartReceiver()
        {
            Receiver.Init(receiveAddress, TransactionSettings, TryProcess, EndProcess);
            Receiver.Start(maximumConcurrencyLevel);
        }

        [DebuggerNonUserCode]
        bool TryProcess(TransportMessage message)
        {
            currentReceivePerformanceDiagnostics.MessageDequeued();

            needToAbort = false;

            if (TransactionSettings.DontUseDistributedTransactions)
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    ProcessMessage(message);
                }
            }
            else
            {
                ProcessMessage(message);
            }

            if (needToAbort)
            {
                return false;
            }

            return true;
        }

        void EndProcess(string messageId, Exception ex)
        {
            throughputLimiter.MessageProcessed();
            
            if (ex == null)
            {
                if (messageId != null)
                {
                    firstLevelRetries.ClearFailuresForMessage(messageId);
                }

                currentReceivePerformanceDiagnostics.MessageProcessed();
            
                return;
            }

            currentReceivePerformanceDiagnostics.MessageFailed();

            if (ex is AggregateException)
            {
                ex = ex.GetBaseException();
            }

            if (TransactionSettings.IsTransactional && messageId != null)
            {
                firstLevelRetries.IncrementFailuresForMessage(messageId, ex);
            }

            OnFailedMessageProcessing(ex);
            
            Logger.Info("Failed to process message", ex);
        }

        void ProcessMessage(TransportMessage m)
        {
            var exceptionFromStartedMessageHandling = OnStartedMessageProcessing(m);

            if (TransactionSettings.IsTransactional)
            {
                if (firstLevelRetries.HasMaxRetriesForMessageBeenReached(m))
                {
                    //HACK: We need this hack here till we refactor the SLR to be a first class concept in the TransactionalTransport
                    if (Configure.Instance.Builder.Build<SecondLevelRetries>().Disabled)
                    {
                        Logger.ErrorFormat("Message has failed the maximum number of times allowed, ID={0}.", m.IdForCorrelation);
                    }
                    else
                    {
                        Logger.WarnFormat("Message has failed the maximum number of times allowed, message will be handed over to SLR, ID={0}.", m.IdForCorrelation);
                    }

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
            if (needToAbort)
            {
                return;
            }

            if (exceptionFromMessageHandling != null)
            {
                if (exceptionFromMessageHandling is AggregateException)
                {
                    var serializationException = exceptionFromMessageHandling.GetBaseException() as  SerializationException;
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
            {
                throw exceptionFromMessageModules;
            }
        }

        /// <summary>
        /// Causes the processing of the current message to be aborted.
        /// </summary>
        public void AbortHandlingCurrentMessage()
        {
            needToAbort = true;
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

        private void OnFailedMessageProcessing(Exception originalException)
        {
            try
            {
                if (FailedMessageProcessing != null)
                    FailedMessageProcessing(this, new FailedMessageProcessingEventArgs(originalException));
            }
            catch (Exception e)
            {
                Logger.Warn("Failed raising 'failed message processing' event.", e);
            }
        }

        Address receiveAddress;
        bool isStarted;
        ThroughputLimiter throughputLimiter;
        FirstLevelRetries firstLevelRetries;

        [ThreadStatic]
        private static volatile bool needToAbort;

        static readonly ILog Logger = LogManager.GetLogger("Transport");

        /// <summary>
        /// Stops all worker threads.
        /// </summary>
        public void Dispose()
        {
            if (!isStarted)
                return;

            Receiver.Stop();
            isStarted = false;
        }

    }
}
