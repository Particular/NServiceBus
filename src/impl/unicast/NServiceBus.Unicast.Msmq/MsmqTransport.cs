using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Common.Logging;
using NServiceBus.Serialization;
using System.Xml.Serialization;
using System.IO;
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
		/// The path to the queue the transport will read from.
		/// Only specify the name of the queue - msmq specific address not required.
		/// When using MSMQ v3, only local queues are supported.
		/// </summary>
        public string InputQueue { get; set; }

		/// <summary>
		/// Sets the path to the queue the transport will transfer
		/// errors to.
		/// </summary>
        public string ErrorQueue { get; set; }

		/// <summary>
		/// Sets whether or not the transport is transactional.
		/// </summary>
        public bool IsTransactional { get; set; }

		/// <summary>
		/// Sets whether or not the transport should deserialize
		/// the body of the message placed on the queue.
		/// </summary>
        public bool SkipDeserialization { get; set; }

	    /// <summary>
	    /// Sets whether or not the transport should purge the input
	    /// queue when it is started.
	    /// </summary>
	    public bool PurgeOnStartup { get; set; }

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

	    private int secondsToWaitForMessage = 10;

        /// <summary>
        /// Sets the maximum interval of time for when a thread thinks there is a message in the queue
        /// that it tries to receive, until it gives up.
        /// 
        /// Default value is 10.
        /// </summary>
        public int SecondsToWaitForMessage
        {
            get { return secondsToWaitForMessage; }
            set { secondsToWaitForMessage = value; }
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
        /// Sets the object which will be used to serialize and deserialize messages.
        /// </summary>
        public IMessageSerializer MessageSerializer { get; set; }

        /// <summary>
        /// Sets the object which will be used for sending and receiving messages.
        /// </summary>
        public IMessageQueue MessageQueue { get; set; }

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
		/// Gets the address of the input queue.
		/// </summary>
        public string Address
        {
            get
            {
                return InputQueue;
            }
        }

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
            CheckConfiguration();
            CreateQueuesIfNecessary();

            MessageQueue.Init(InputQueue, PurgeOnStartup, SecondsToWaitForMessage);

            if (!string.IsNullOrEmpty(InputQueue))
            {
                for (int i = 0; i < numberOfWorkerThreads; i++)
                    AddWorkerThread().Start();
            }
        }

        private void CheckConfiguration()
        {
            if (MessageSerializer == null)
                throw new InvalidOperationException("No message serializer has been configured.");
        }

        private void CreateQueuesIfNecessary()
        {
            if (!DoNotCreateQueues)
            {
                MessageQueue.CreateQueue(InputQueue);
                MessageQueue.CreateQueue(ErrorQueue);
            }
        }

	    /// <summary>
		/// Re-queues a message for processing at another time.
		/// </summary>
		/// <param name="m">The message to process later.</param>
		/// <remarks>
		/// This method will place the message onto the back of the queue
		/// which may break message ordering.
		/// </remarks>
        public void ReceiveMessageLater(TransportMessage m)
        {
            if (!string.IsNullOrEmpty(InputQueue))
                Send(m, InputQueue);
        }

		/// <summary>
		/// Sends a message to the specified destination.
		/// </summary>
		/// <param name="m">The message to send.</param>
		/// <param name="destination">The address of the destination to send the message to.</param>
        public void Send(TransportMessage m, string destination)
        {
		    var toSend = Convert(m);

            try
            {
                MessageQueue.Send(toSend, destination, IsTransactional);
            }
            catch(QueueNotFoundException ex)
            {
                throw new ConfigurationException("The destination queue '" + destination +
                                                     "' could not be found. You may have misconfigured the destination for this kind of message (" +
                                                     m.Body[0].GetType().FullName +
                                                     ") in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file." +
                                                     "It may also be the case that the given queue just hasn't been created yet, or has been deleted."
                                                    , ex);
            }

            m.Id = toSend.Id;
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
		    var message = Peek();
            if (message == null)
                return;

		    _needToAbort = false;
		    _messageId = string.Empty;

            try
            {
                if (IsTransactional)
                    new TransactionWrapper().RunInTransaction(() => ProcessMessage(message), IsolationLevel, TransactionTimeout);
                else
                    ProcessMessage(message);

                ClearFailuresForMessage(_messageId);
            }
            catch (AbortHandlingCurrentMessageException)
            {
                //in case AbortHandlingCurrentMessage was called
                return; //don't increment failures, we want this message kept around.
            }
            catch
            {
                if (IsTransactional)
                    IncrementFailuresForMessage(_messageId);

                OnFailedMessageProcessing();
            }
        }

	    /// <summary>
		/// Receives a message from the input queue.
		/// </summary>
		/// <remarks>
		/// If a message is received the <see cref="TransportMessageReceived"/> event will be raised.
		/// </remarks>
        public void ProcessMessage(QueuedMessage m)
        {
	        RemoveQueuedMessage(m);

            _messageId = m.Id;

            if (IsTransactional)
            {
                if (HandledMaxRetries(m.Id))
                {
                    MoveToErrorQueue(m);
                    return;
                }                    
            }

            //exceptions here will cause a rollback - which is what we want.
            if (StartedMessageProcessing != null)
                StartedMessageProcessing(this, null);

            var result = Convert(m);

            if (SkipDeserialization)
                result.BodyStream = m.BodyStream;
            else
            {
                try
                {
                    result.Body = Extract(m);
                }
                catch (Exception e)
                {
                    Logger.Error("Could not extract message data.", e);

                    MoveToErrorQueue(m);

                    OnFinishedMessageProcessing(); // don't care about failures here
                    return; // deserialization failed - no reason to try again, so don't throw
                }
            }

            //care about failures here
            var exceptionNotThrown = OnTransportMessageReceived(result);
            //and here
            var otherExNotThrown = OnFinishedMessageProcessing();

            //but need to abort takes precedence - failures aren't counted here,
            //so messages aren't moved to the error queue.
            if (_needToAbort)
                throw new AbortHandlingCurrentMessageException();

            if (!(exceptionNotThrown && otherExNotThrown)) //cause rollback
                throw new ApplicationException("Exception occured while processing message.");
        }

        private bool HandledMaxRetries(string messageId)
        {
            failuresPerMessageLocker.EnterReadLock();

            if (failuresPerMessage.ContainsKey(messageId) &&
                   (failuresPerMessage[messageId] >= maxRetries))
            {
                failuresPerMessageLocker.ExitReadLock();
                failuresPerMessageLocker.EnterWriteLock();
                failuresPerMessage.Remove(messageId);
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
	            failuresPerMessageLocker.ExitWriteLock();
	        }
	        else
	            failuresPerMessageLocker.ExitReadLock();
	    }

	    private void IncrementFailuresForMessage(string messageId)
	    {
	        failuresPerMessageLocker.EnterWriteLock();
	        try
	        {
	            if (!failuresPerMessage.ContainsKey(messageId))
	                failuresPerMessage[messageId] = 1;
	            else
	                failuresPerMessage[messageId] = failuresPerMessage[messageId] + 1;
	        }
	        finally
	        {
	            failuresPerMessageLocker.ExitWriteLock();
	        }
	    }

	    [DebuggerNonUserCode] // so that exceptions don't interfere with debugging.
        private QueuedMessage Peek()
        {
            try
            {
                return MessageQueue.Peek();
            }
            catch (ObjectDisposedException)
            {
                Logger.Fatal("Queue has been disposed. Cannot continue operation. Please restart this process.");
                return null;
            }
            catch (Exception e)
            {
                Logger.Error("Error in peeking a message from queue.", e);
                return null;
            }
        }

        [DebuggerNonUserCode] // so that exceptions don't interfere with debugging.
        private void RemoveQueuedMessage(QueuedMessage m)
        {
            try
            {
                MessageQueue.RemoveQueuedMessage(m, IsTransactional);
            }
            catch (ObjectDisposedException)
            {
                Logger.Fatal("Queue has been disposed. Cannot continue operation. Please restart this process.");
                return;
            }
            catch(Exception e)
            {
                Logger.Error("Error in receiving message from queue.", e);
                return;
            }
        }

        /// <summary>
        /// Moves the given message to the configured error queue.
        /// </summary>
        /// <param name="m"></param>
	    protected void MoveToErrorQueue(QueuedMessage m)
	    {
            m.Label = m.Label +
                      string.Format("<{0}>{1}</{0}>", FAILEDQUEUE, InputQueue);

	        if (ErrorQueue != null)
	            MessageQueue.Send(m, ErrorQueue, false);
	    }

        /// <summary>
        /// Causes the processing of the current message to be aborted.
        /// </summary>
	    public void AbortHandlingCurrentMessage()
        {
            _needToAbort = true;
        }

		/// <summary>
        /// Converts a <see cref="QueuedMessage"/> into an NServiceBus message.
		/// </summary>
        /// <param name="m">The QueuedMessage to convert.</param>
		/// <returns>An NServiceBus message.</returns>
        public TransportMessage Convert(QueuedMessage m)
        {
		    var result = new TransportMessage
		                     {
		                         Id = m.Id,
		                         CorrelationId =
		                             (m.CorrelationId == "00000000-0000-0000-0000-000000000000\\0"
		                                  ? null
		                                  : m.CorrelationId),
		                         Recoverable = m.Recoverable,
		                         TimeToBeReceived = m.TimeToBeReceived,
                                 TimeSent = m.TimeSent,
                                 ReturnAddress = m.ResponseQueue,
		                         MessageIntent = Enum.IsDefined(typeof(MessageIntentEnum), m.AppSpecific) ? (MessageIntentEnum)m.AppSpecific : MessageIntentEnum.Send
                             };

		    FillIdForCorrelationAndWindowsIdentity(result, m);

            if (string.IsNullOrEmpty(result.IdForCorrelation))
                result.IdForCorrelation = result.Id;

            if (m.Extension != null && m.Extension.Length > 0)
            {
                var stream = new MemoryStream(m.Extension);
                var o = headerSerializer.Deserialize(stream);
                result.Headers = o as List<HeaderInfo>;
            }
            else
            {
                result.Headers = new List<HeaderInfo>();
            }

		    return result;
        }

        /// <summary>
        /// Converts the given message to its QueuedMessage equivalent.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public QueuedMessage Convert(TransportMessage message)
        {
            var result = new QueuedMessage
                             {
                                 AppSpecific = (int)message.MessageIntent,
                                 CorrelationId = message.CorrelationId,
                                 Label = GetLabel(message),
                                 Recoverable = message.Recoverable,
                                 ResponseQueue = message.ReturnAddress,
                                 TimeToBeReceived = message.TimeToBeReceived
                             };

            if (message.Body == null && message.BodyStream != null)
                result.BodyStream = message.BodyStream;
            else
            {
                result.BodyStream = new MemoryStream();
                MessageSerializer.Serialize(message.Body, result.BodyStream);
            }

            if (message.Headers != null && message.Headers.Count > 0)
            {
                using (var stream = new MemoryStream())
                {
                    headerSerializer.Serialize(stream, message.Headers);
                    result.Extension = stream.GetBuffer();
                }
            }

            return result;
        }
        
        /// <summary>
        /// Returns the queue whose process failed processing the given message
        /// by accessing the label of the message.
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public static string GetFailedQueue(string label)
        {
            if (label == null)
                return null;

            if (!label.Contains(FAILEDQUEUE))
                return null;

            var startIndex = label.IndexOf(string.Format("<{0}>", FAILEDQUEUE)) + FAILEDQUEUE.Length + 2;
            var count = label.IndexOf(string.Format("</{0}>", FAILEDQUEUE)) - startIndex;

            return label.Substring(startIndex, count);
        }

        /// <summary>
        /// Gets the label of the message stripping out the failed queue.
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public static string GetLabelWithoutFailedQueue(string label)
        {
            if (label == null)
                return null;

            if (!label.Contains(FAILEDQUEUE))
                return label;

            var startIndex = label.IndexOf(string.Format("<{0}>", FAILEDQUEUE));
            var endIndex = label.IndexOf(string.Format("</{0}>", FAILEDQUEUE));
            endIndex += FAILEDQUEUE.Length + 3;

            return label.Remove(startIndex, endIndex - startIndex);
        }

        private static void FillIdForCorrelationAndWindowsIdentity(TransportMessage result, QueuedMessage m)
        {
            if (m.Label == null)
                return;

            if (m.Label.Contains(IDFORCORRELATION))
            {
                int idStartIndex = m.Label.IndexOf(string.Format("<{0}>", IDFORCORRELATION)) + IDFORCORRELATION.Length + 2;
                int idCount = m.Label.IndexOf(string.Format("</{0}>", IDFORCORRELATION)) - idStartIndex;

                result.IdForCorrelation = m.Label.Substring(idStartIndex, idCount);
            }

            if (m.Label.Contains(WINDOWSIDENTITYNAME))
            {
                int winStartIndex = m.Label.IndexOf(string.Format("<{0}>", WINDOWSIDENTITYNAME)) + WINDOWSIDENTITYNAME.Length + 2;
                int winCount = m.Label.IndexOf(string.Format("</{0}>", WINDOWSIDENTITYNAME)) - winStartIndex;

                result.WindowsIdentityName = m.Label.Substring(winStartIndex, winCount);
            }
        }

        private static string GetLabel(TransportMessage m)
        {
            return string.Format("<{0}>{2}</{0}><{1}>{3}</{1}>",IDFORCORRELATION, WINDOWSIDENTITYNAME, m.IdForCorrelation, m.WindowsIdentityName);
        }

        /// <summary>
        /// Extracts the messages from a <see cref="QueuedMessage"/>.
        /// </summary>
        /// <param name="message">The message to extract from.</param>
        /// <returns>An array of handleable messages.</returns>
        private IMessage[] Extract(QueuedMessage message)
        {
            return MessageSerializer.Deserialize(message.BodyStream);
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
                Logger.Error("Failed raising 'transport message received' event.", e);
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
                Logger.Error("Failed raising 'failed message processing' event.", e);
                return false;
            }

            return true;
        }

        #endregion

        #region members

	    private static readonly string IDFORCORRELATION = "CorrId";
	    private static readonly string WINDOWSIDENTITYNAME = "WinIdName";
	    private static readonly string FAILEDQUEUE = "FailedQ";

        private readonly IList<WorkerThread> workerThreads = new List<WorkerThread>();

        private readonly ReaderWriterLockSlim failuresPerMessageLocker = new ReaderWriterLockSlim();
        /// <summary>
        /// Accessed by multiple threads - lock using failuresPerMessageLocker.
        /// </summary>
	    private readonly IDictionary<string, int> failuresPerMessage = new Dictionary<string, int>();

	    [ThreadStatic] 
        private static volatile bool _needToAbort;

	    [ThreadStatic] private static volatile string _messageId;

        private static readonly ILog Logger = LogManager.GetLogger(typeof (MsmqTransport));

        private readonly XmlSerializer headerSerializer = new XmlSerializer(typeof(List<HeaderInfo>));
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
