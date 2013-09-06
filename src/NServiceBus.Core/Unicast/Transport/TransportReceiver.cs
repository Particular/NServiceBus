namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Transactions;
    using Faults;
    using Logging;
    using Monitoring;
    using Transports;

    /// <summary>
    /// The default implementation of <see cref="ITransport"/>
    /// </summary>
    public class TransportReceiver : ITransport, IDisposable
    {
        /// <summary>
        /// The receiver responsible for notifying the transport when new messages are available
        /// </summary>
        public IDequeueMessages Receiver { get; set; }

        /// <summary>
        /// Manages failed message processing.
        /// </summary>
        public IManageMessageFailures FailureManager { get; set; }

        /// <summary>
        /// Event which indicates that message processing has started.
        /// </summary>
        public event EventHandler<StartedMessageProcessingEventArgs> StartedMessageProcessing;

        /// <summary>
        /// The current settings for transactions
        /// </summary>
        public TransactionSettings TransactionSettings { get; set; }

        /// <summary>
        /// Event which indicates that message processing has completed.
        /// </summary>
        public event EventHandler<FinishedMessageProcessingEventArgs> FinishedMessageProcessing;

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
            Start(Address.Parse(inputqueue));
        }

        public void Start(Address address)
        {
            if (isStarted)
                throw new InvalidOperationException("The transport is already started");

            receiveAddress = address;

            var returnAddressForFailures = address;

            if (Configure.Instance.WorkerRunsOnThisEndpoint() 
                && (returnAddressForFailures.Queue.ToLower().EndsWith(".worker")|| address ==Address.Local )) //this is a hack until we can refactor the SLR to be a feature. "Worker" is there to catch the local worker in the distributor
            {
                returnAddressForFailures = Configure.Instance.GetMasterNodeAddress();

                Logger.InfoFormat("Worker started, failures will be redirected to {0}",returnAddressForFailures);
            }

            FailureManager.Init(returnAddressForFailures);

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

            using (var tx = GetTransactionScope())
            {
                ProcessMessage(message);
                
                tx.Complete();
            }
            
            return !needToAbort;
        }

        TransactionScope GetTransactionScope()
        {
            if (TransactionSettings.DoNotWrapHandlersExecutionInATransactionScope)
            {
                return new TransactionScope(TransactionScopeOption.Suppress);
            }

            return new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
                {
                    IsolationLevel = TransactionSettings.IsolationLevel,
                    Timeout = TransactionSettings.TransactionTimeout
                });
        }


        void EndProcess(TransportMessage message, Exception ex)
        {
            var messageId = message != null ? message.Id : null;

            if (needToAbort)
                return;

            throughputLimiter.MessageProcessed();
            
            if (ex == null)
            {
                if (messageId != null)
                {
                    firstLevelRetries.ClearFailuresForMessage(message);
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
                firstLevelRetries.IncrementFailuresForMessage(message, ex);
            }

            OnFailedMessageProcessing(message, ex);
            
            Logger.Info("Failed to process message", ex);
        }

        void ProcessMessage(TransportMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.Id))
            {
                Logger.Error("Message without message id detected");

                FailureManager.SerializationFailedForMessage(message, new SerializationException("Message without message id received."));
                
                return;
            }

            var exceptionFromStartedMessageHandling = OnStartedMessageProcessing(message);

            if (TransactionSettings.IsTransactional)
            {
                if (firstLevelRetries.HasMaxRetriesForMessageBeenReached(message))
                {
                    OnFinishedMessageProcessing(message);
                    return;
                }
            }

            if (exceptionFromStartedMessageHandling != null)
                throw exceptionFromStartedMessageHandling; //cause rollback 

            //care about failures here
            var exceptionFromMessageHandling = OnTransportMessageReceived(message);

            //and here
            var exceptionFromMessageModules = OnFinishedMessageProcessing(message);

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
                        Logger.Error("Failed to serialize message with ID: " + message.Id, serializationException);

                        message.RevertToOriginalBodyIfNeeded();

                        FailureManager.SerializationFailedForMessage(message, serializationException);
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

        /// <summary>
        /// Stops the transport.
        /// </summary>
        public void Stop()
        {
            if (!isStarted)
            {
                return;
            }

            Receiver.Stop();

            isStarted = false;
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

        private Exception OnFinishedMessageProcessing(TransportMessage msg)
        {
            try
            {
                if (FinishedMessageProcessing != null)
                    FinishedMessageProcessing(this, new FinishedMessageProcessingEventArgs(msg));
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

        private void OnFailedMessageProcessing(TransportMessage message, Exception originalException)
        {
            try
            {
                if (FailedMessageProcessing != null)
                    FailedMessageProcessing(this, new FailedMessageProcessingEventArgs(message, originalException));
            }
            catch (Exception e)
            {
                Logger.Warn("Failed raising 'failed message processing' event.", e);
            }
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void DisposeManaged()
        {
            Stop();

            if (currentReceivePerformanceDiagnostics != null)
            {
                currentReceivePerformanceDiagnostics.Dispose();
            }
        }


        Address receiveAddress;
        bool isStarted;
        ThroughputLimiter throughputLimiter;
        FirstLevelRetries firstLevelRetries;

        [ThreadStatic]
        static volatile bool needToAbort;

        static readonly ILog Logger = LogManager.GetLogger(typeof(TransportReceiver));
    }
}
