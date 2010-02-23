using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Common.Logging;
using NServiceBus.Faults;
using System.Diagnostics;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Utils;

namespace NServiceBus.Unicast.Transport.Msmq
{
	/// <summary>
	/// An MSMQ implementation of <see cref="ITransport"/> for use with
	/// NServiceBus.
	/// </summary>
	/// <remarks>
	/// A transport is used by NServiceBus as a high level abstraction from the 
	/// underlying messaging service being used to transfer messages.
	/// </remarks>
    public class MsmqTransport : ITransport
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
        /// Property indicating that queues will not be created on startup
        /// if they do not already exist.
        /// </summary>
        public bool DoNotCreateQueues { get; set; }

        /// <summary>
        /// Sets the object which will be used for sending and receiving messages.
        /// </summary>
        public IMessageQueue MessageQueue { get; set; }

        /// <summary>
        /// Manages failed message processing.
        /// </summary>
        public IManageMessageFailures FailureManager { get; set; }

        #endregion

        #region ITransport Members

        /// <summary>
        /// Event which indicates that message processing has started.
        /// </summary>
        public event EventHandler StartedMessageProcessing;

        /// <summary>
        /// Event which indicates that message processing has completed.
        /// </summary>
        public event EventHandler FinishedMessageProcessing;

        /// <summary>
        /// Event which indicates that message processing failed for some reason.
        /// </summary>
	    public event EventHandler FailedMessageProcessing;

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
        private int numberOfWorkerThreads;


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

	    /// <summary>
		/// Starts the transport.
		/// </summary>
        public void Start()
        {
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
            if (!HasMessage())
                return;

		    _needToAbort = false;
		    _messageId = string.Empty;

            try
            {
                if (IsTransactional)
                    new TransactionWrapper().RunInTransaction(ProcessMessage, IsolationLevel, TransactionTimeout);
                else
                    ProcessMessage();

                ClearFailuresForMessage(_messageId);
            }
            catch (AbortHandlingCurrentMessageException)
            {
                //in case AbortHandlingCurrentMessage was called
                return; //don't increment failures, we want this message kept around.
            }
            catch(Exception e)
            {
                if (IsTransactional)
                {
                    IncrementFailuresForMessage(_messageId, e);
                }

                OnFailedMessageProcessing();
            }
        }

	    /// <summary>
		/// Receives a message from the input queue.
		/// </summary>
		/// <remarks>
		/// If a message is received the <see cref="TransportMessageReceived"/> event will be raised.
		/// </remarks>
        public void ProcessMessage()
        {
	        var m = Receive();
            if (m == null)
                return;

            _messageId = m.Id;

            if (IsTransactional)
            {
                if (HandledMaxRetries(m))
                {
                    Logger.Error(string.Format("Message has failed the maximum number of times allowed, ID={0}.", m.Id));
                    return;
                }
            }

            //exceptions here will cause a rollback - which is what we want.
            if (StartedMessageProcessing != null)
                StartedMessageProcessing(this, null);

            //care about failures here
            var exceptionNotThrown = OnTransportMessageReceived(m);
            //and here
            var otherExNotThrown = OnFinishedMessageProcessing();

            //but need to abort takes precedence - failures aren't counted here,
            //so messages aren't moved to the error queue.
            if (_needToAbort)
                throw new AbortHandlingCurrentMessageException();

            if (!(exceptionNotThrown && otherExNotThrown)) //cause rollback
                throw new ApplicationException("Exception occured while processing message.");
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
                FailureManager.ProcessingAlwaysFailsForMessage(message, ex);

                failuresPerMessage.Remove(messageId);
                exceptionsForMessages.Remove(messageId);

                failuresPerMessageLocker.ExitWriteLock();

                return true;
            }

            failuresPerMessageLocker.ExitReadLock();
            return false;
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
	        failuresPerMessageLocker.EnterWriteLock();
	        try
	        {
	            if (!failuresPerMessage.ContainsKey(messageId))
	                failuresPerMessage[messageId] = 1;
	            else
	                failuresPerMessage[messageId] = failuresPerMessage[messageId] + 1;

                exceptionsForMessages[messageId] = e;
	        }
	        finally
	        {
	            failuresPerMessageLocker.ExitWriteLock();
	        }
	    }

	    [DebuggerNonUserCode] // so that exceptions don't interfere with debugging.
        private bool HasMessage()
        {
            try
            {
                return MessageQueue.HasMessage();
            }
            catch (ObjectDisposedException)
            {
                Logger.Fatal("Queue has been disposed. Cannot continue operation. Please restart this process.");
                return false;
            }
            catch (Exception e)
            {
                Logger.Error("Error in peeking a message from queue.", e);
                return false;
            }
        }

        [DebuggerNonUserCode] // so that exceptions don't interfere with debugging.
        private TransportMessage Receive()
        {
            try
            {
                return MessageQueue.Receive(IsTransactional);
            }
            catch (ObjectDisposedException)
            {
                Logger.Fatal("Queue has been disposed. Cannot continue operation. Please restart this process.");
                return null;
            }
            catch(Exception e)
            {
                Logger.Error("Error in receiving message from queue.", e);
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

        private bool OnFinishedMessageProcessing()
        {
            try
            {
                if (FinishedMessageProcessing != null)
                    FinishedMessageProcessing(this, null);
            }
            catch (Exception e)
            {
                Logger.Error("Failed raising 'finished message processing' event.", e);
                return false;
            }

            return true;
        }

        private bool OnTransportMessageReceived(TransportMessage msg)
        {
            try
            {
                if (TransportMessageReceived != null)
                    TransportMessageReceived(this, new TransportMessageReceivedEventArgs(msg));
            }
            catch (Exception e)
            {
                Logger.Warn("Failed raising 'transport message received' event for message with ID=" + msg.Id, e);
                return false;
            }

            return true;
        }

        private bool OnFailedMessageProcessing()
        {
            try
            {
                if (FailedMessageProcessing != null)
                    FailedMessageProcessing(this, null);
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

	    [ThreadStatic] private static volatile string _messageId;

        private static readonly ILog Logger = LogManager.GetLogger(typeof (MsmqTransport));

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
