namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Diagnostics;
    using System.Runtime.ExceptionServices;
    using System.Runtime.Serialization;
    using System.Transactions;
    using Faults;
    using Logging;
    using Monitoring;
    using Settings;
    using Transports;

    /// <summary>
    ///     The default implementation of <see cref="ITransport" />
    /// </summary>
    public class TransportReceiver : ITransport, IDisposable
    {
        /// <summary>
        /// Creates an instance of <see cref="TransportReceiver"/>
        /// </summary>
        /// <param name="transactionSettings">The transaction settings to use for this <see cref="TransportReceiver"/>.</param>
        /// <param name="maximumConcurrencyLevel">The maximum number of messages to process in parallel.</param>
        /// <param name="maximumThroughput">The maximum throughput per second, 0 means unlimited.</param>
        /// <param name="receiver">The <see cref="IDequeueMessages"/> instance to use.</param>
        /// <param name="manageMessageFailures">The <see cref="IManageMessageFailures"/> instance to use.</param>
        /// <param name="settings">The current settings</param>
        public TransportReceiver(TransactionSettings transactionSettings, int maximumConcurrencyLevel, int maximumThroughput, IDequeueMessages receiver, IManageMessageFailures manageMessageFailures,ReadOnlySettings settings)
        {
            this.settings = settings;
            TransactionSettings = transactionSettings;
            MaximumConcurrencyLevel = maximumConcurrencyLevel;
            MaximumMessageThroughputPerSecond = maximumThroughput;
            FailureManager = manageMessageFailures;
            Receiver = receiver;
        }

        /// <summary>
        ///     The receiver responsible for notifying the transport when new messages are available
        /// </summary>
        public IDequeueMessages Receiver { get; private set; }

        /// <summary>
        ///     Manages failed message processing.
        /// </summary>
        public IManageMessageFailures FailureManager { get; set; }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            //Injected at compile time
        }

        /// <summary>
        ///     Event which indicates that message processing has started.
        /// </summary>
        public event EventHandler<StartedMessageProcessingEventArgs> StartedMessageProcessing;

        /// <summary>
        ///     Event which indicates that message processing has completed.
        /// </summary>
        public event EventHandler<FinishedMessageProcessingEventArgs> FinishedMessageProcessing;

        /// <summary>
        ///     Event which indicates that message processing failed for some reason.
        /// </summary>
        public event EventHandler<FailedMessageProcessingEventArgs> FailedMessageProcessing;

        /// <summary>
        ///     Gets the maximum concurrency level this <see cref="ITransport" /> is able to support.
        /// </summary>
        public virtual int MaximumConcurrencyLevel { get; private set; }

        /// <summary>
        ///     Updates the maximum concurrency level this <see cref="ITransport" /> is able to support.
        /// </summary>
        /// <param name="maximumConcurrencyLevel">The new maximum concurrency level for this <see cref="ITransport" />.</param>
        public void ChangeMaximumConcurrencyLevel(int maximumConcurrencyLevel)
        {
            if (MaximumConcurrencyLevel == maximumConcurrencyLevel)
            {
                return;
            }

            MaximumConcurrencyLevel = maximumConcurrencyLevel;

            if (isStarted)
            {
                Receiver.Stop();
                Receiver.Start(maximumConcurrencyLevel);
                Logger.InfoFormat("Maximum concurrency level for '{0}' changed to {1}.", receiveAddress,
                    maximumConcurrencyLevel);
            }
        }

        /// <summary>
        ///     Gets the receiving messages rate.
        /// </summary>
        public int MaximumMessageThroughputPerSecond { get; private set; }

        /// <summary>
        /// Updates the MaximumMessageThroughputPerSecond setting.
        /// </summary>
        /// <param name="maximumMessageThroughputPerSecond">The new value.</param>
        public void ChangeMaximumMessageThroughputPerSecond(int maximumMessageThroughputPerSecond)
        {
            if (maximumMessageThroughputPerSecond == MaximumMessageThroughputPerSecond)
            {
                return;
            }

            lock (changeMaximumMessageThroughputPerSecondLock)
            {
                MaximumMessageThroughputPerSecond = maximumMessageThroughputPerSecond;
                if (throughputLimiter != null)
                {
                    throughputLimiter.Stop();
                    throughputLimiter.Start(maximumMessageThroughputPerSecond);
                }
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
        ///     Event raised when a message has been received in the input queue.
        /// </summary>
        public event EventHandler<TransportMessageReceivedEventArgs> TransportMessageReceived;


        /// <summary>
        /// Starts the transport listening for messages on the given local address.
        /// </summary>
        public void Start(Address address)
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            receiveAddress = address;

            var returnAddressForFailures = address;

            var workerRunsOnThisEndpoint = settings.GetOrDefault<bool>("Worker.Enabled");

            if (workerRunsOnThisEndpoint
                && (returnAddressForFailures.Queue.ToLower().EndsWith(".worker") || address == Address.Local))
                //this is a hack until we can refactor the SLR to be a feature. "Worker" is there to catch the local worker in the distributor
            {
                returnAddressForFailures = settings.Get<Address>("MasterNode.Address");

                Logger.InfoFormat("Worker started, failures will be redirected to {0}", returnAddressForFailures);
            }

            FailureManager.Init(returnAddressForFailures);

            firstLevelRetries = new FirstLevelRetries(TransactionSettings.MaxRetries, FailureManager, CriticalError);

            InitializePerformanceCounters();

            throughputLimiter = new ThroughputLimiter();

            throughputLimiter.Start(MaximumMessageThroughputPerSecond);

            StartReceiver();

            if (MaximumMessageThroughputPerSecond > 0)
            {
                Logger.InfoFormat("Transport: {0} started with its throughput limited to {1} msg/sec", receiveAddress,
                    MaximumMessageThroughputPerSecond);
            }

            isStarted = true;
        }

        /// <summary>
        ///     Causes the processing of the current message to be aborted.
        /// </summary>
        public void AbortHandlingCurrentMessage()
        {
            needToAbort = true;
        }

        /// <summary>
        ///     Stops the transport.
        /// </summary>
        public void Stop()
        {
            InnerStop();
        }

        void InitializePerformanceCounters()
        {
            currentReceivePerformanceDiagnostics = new ReceivePerformanceDiagnostics(receiveAddress);

            currentReceivePerformanceDiagnostics.Initialize();
        }

        void StartReceiver()
        {
            Receiver.Init(receiveAddress, TransactionSettings, TryProcess, EndProcess);
            Receiver.Start(MaximumConcurrencyLevel);
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
            {
                return;
            }

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

                FailureManager.SerializationFailedForMessage(message,
                    new SerializationException("Message without message id received."));

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
            {
                ExceptionDispatchInfo.Capture(exceptionFromStartedMessageHandling)
                    .Throw();
            }

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
                    var serializationException =
                        exceptionFromMessageHandling.GetBaseException() as SerializationException;
                    if (serializationException != null)
                    {
                        Logger.Error("Failed to deserialize message with ID: " + message.Id, serializationException);

                        message.RevertToOriginalBodyIfNeeded();

                        FailureManager.SerializationFailedForMessage(message, serializationException);
                    }
                    else
                    {
                        ExceptionDispatchInfo.Capture(exceptionFromMessageHandling)
                            .Throw();
                    }
                }
                else
                {
                    ExceptionDispatchInfo.Capture(exceptionFromMessageHandling)
                        .Throw();
                }
            }

            if (exceptionFromMessageModules != null) //cause rollback
            {
                ExceptionDispatchInfo.Capture(exceptionFromMessageModules)
                    .Throw();
            }
        }


        void InnerStop()
        {
            if (!isStarted)
            {
                return;
            }

            Receiver.Stop();
            throughputLimiter.Stop();

            isStarted = false;
        }

        Exception OnStartedMessageProcessing(TransportMessage msg)
        {
            try
            {
                if (StartedMessageProcessing != null)
                {
                    StartedMessageProcessing(this, new StartedMessageProcessingEventArgs(msg));
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed raising 'started message processing' event.", e);
                return e;
            }

            return null;
        }

        Exception OnFinishedMessageProcessing(TransportMessage msg)
        {
            try
            {
                if (FinishedMessageProcessing != null)
                {
                    FinishedMessageProcessing(this, new FinishedMessageProcessingEventArgs(msg));
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed raising 'finished message processing' event.", e);
                return e;
            }

            return null;
        }

        Exception OnTransportMessageReceived(TransportMessage msg)
        {
            try
            {
                if (TransportMessageReceived != null)
                {
                    TransportMessageReceived(this, new TransportMessageReceivedEventArgs(msg));
                }
            }
            catch (Exception e)
            {
                return e;
            }

            return null;
        }

        void OnFailedMessageProcessing(TransportMessage message, Exception originalException)
        {
            try
            {
                if (FailedMessageProcessing != null)
                {
                    FailedMessageProcessing(this, new FailedMessageProcessingEventArgs(message, originalException));
                }
            }
            catch (Exception e)
            {
                Logger.Warn("Failed raising 'failed message processing' event.", e);
            }
        }

        void DisposeManaged()
        {
            InnerStop();

            if (currentReceivePerformanceDiagnostics != null)
            {
                currentReceivePerformanceDiagnostics.Dispose();
            }
        }


        [ThreadStatic] static volatile bool needToAbort;

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();
        object changeMaximumMessageThroughputPerSecondLock = new object();
        ReceivePerformanceDiagnostics currentReceivePerformanceDiagnostics;
        FirstLevelRetries firstLevelRetries;
        bool isStarted;
        Address receiveAddress;
        ThroughputLimiter throughputLimiter;
        readonly ReadOnlySettings settings;


        /// <summary>
        /// The <see cref="TransactionSettings"/> being used.
        /// </summary>
        public TransactionSettings TransactionSettings { get; private set; }

        internal CriticalError CriticalError { get; set; }
    }
}
