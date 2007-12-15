using System;
using System.Collections.Generic;
using System.Messaging;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Transactions;
using System.Xml.Serialization;
using Common.Logging;
using NServiceBus.Messages;
using Utils;

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
    public class MsmqTransport : ITransport, IDisposable
    {
        #region config info

		/// <summary>
		/// Sets the path to the queue the transport will read from.
		/// </summary>
		/// <exception cref="ApplicationException">
		/// Thrown if the queue specified is not a local queue.
		/// </exception>
        public string InputQueue
        {
            set
            {
                MessageQueue q = new MessageQueue(value);

                if (!QueueIsLocal(value))
                    throw new ApplicationException("Cannot work transactionally with remote queue.");
                else
                    SetLocalQueue(q);
            }
        }

		/// <summary>
		/// Sets the path to the queue the transport will transfer
		/// errors to.
		/// </summary>
        public string ErrorQueue
        {
            set
            {
                this.errorQueue = new MessageQueue(value);
            }
        }

        private bool useXmlSerialization;

		/// <summary>
		/// Sets whether or not Xml serialization should be used for 
		/// the body of the message when placing it onto the queue.
		/// </summary>
        public bool UseXmlSerialization
        {
            set { useXmlSerialization = value; }
        }

        private bool isTransactional;

		/// <summary>
		/// Sets whether or not the transport is transactional.
		/// </summary>
        public bool IsTransactional
        {
            set { this.isTransactional = value; }
        }

        private bool skipDeserialization;

		/// <summary>
		/// Sets whether or not the transport should deserialize
		/// the body of the message placed on the queue.
		/// </summary>
        public bool SkipDeserialization
        {
            set { skipDeserialization = value; }
        }

        private bool purgeOnStartup = false;

		/// <summary>
		/// Sets whether or not the transport should purge the input
		/// queue when it is started.
		/// </summary>
        public bool PurgeOnStartup
        {
            set { purgeOnStartup = value; }
        }

        private int numberOfWorkerThreads;

		/// <summary>
		/// Sets the number of concurrent threads that should be
		/// created for processing the queue.
		/// </summary>
        public int NumberOfWorkerThreads
        {
            set { numberOfWorkerThreads = value; }
        }

        private string distributorControlAddress;

		/// <summary>
		/// Sets the address of the distributor control queue.
		/// </summary>
		/// <remarks>
		/// Used in the <see cref="Receive"/> method. Notifies the given distributor
		/// when a thread is now available to handle a new message.
		/// </remarks>
        public string DistributorControlAddress
        {
            set { distributorControlAddress = value; }
        }

        #endregion

        #region ITransport Members

		/// <summary>
		/// Event raised when a message has been received in the input queue.
		/// </summary>
        public event EventHandler<MsgReceivedEventArgs> MsgReceived;

		/// <summary>
		/// Gets the address of the input queue.
		/// </summary>
        public string Address
        {
            get
            {
                return this.queue.Path;
            }
        }

		/// <summary>
		/// Sets a list of the message types the transport will receive.
		/// </summary>
        public IList<Type> MessageTypesToBeReceived
        {
            set
            {
                if (this.useXmlSerialization)
                {
                    Type[] messageTypes = GetExtraTypes(value);

                    this.xmlSerializer = new XmlSerializer(typeof(object), new List<Type>(messageTypes).ToArray());
                }
            }
        }

		/// <summary>
		/// Starts the transport.
		/// </summary>
        public void Start()
        {
            if (this.purgeOnStartup)
                this.queue.Purge();

            lock(readyMessageLocker)
                readyMessageStartup = true;

            for (int i = 0; i < this.numberOfWorkerThreads; i++)
                this.workerThreads.Add(new WorkerThread(this.Receive));

            foreach (WorkerThread w in this.workerThreads)
                w.Start();
        }

		/// <summary>
		/// Re-queues a message for processing at another time.
		/// </summary>
		/// <param name="m">The message to process later.</param>
		/// <remarks>
		/// Note that this method will place the message onto the back of the queue
		/// which will break message ordering.
		/// </remarks>
        public void ReceiveMessageLater(Msg m)
        {
            this.Send(m, this.Address);
        }

		/// <summary>
		/// Sends a message to the specified destination.
		/// </summary>
		/// <param name="m">The message to send.</param>
		/// <param name="destination">The address of the destination to send the message to.</param>
        public void Send(Msg m, string destination)
        {
            string address = Resolve(destination);

            using (MessageQueue q = new MessageQueue(address, QueueAccessMode.Send))
            {
                Message toSend = new Message();

                if (m.Body == null && m.BodyStream != null)
                    toSend.BodyStream = m.BodyStream;
                else
                {
                    if (this.useXmlSerialization)
                        this.xmlSerializer.Serialize(toSend.BodyStream, new List<object>(m.Body));
                    else
                        this.binaryFormatter.Serialize(toSend.BodyStream, new List<object>(m.Body));
                }

                if (m.CorrelationId != null)
                    toSend.CorrelationId = m.CorrelationId;

                toSend.Recoverable = m.Recoverable;
                toSend.ResponseQueue = new MessageQueue(m.ReturnAddress);
                toSend.Label = m.WindowsIdentityName;

                if (m.TimeToBeReceived < MessageQueue.InfiniteTimeout)
                    toSend.TimeToBeReceived = m.TimeToBeReceived;

                q.Send(toSend, this.GetTransactionTypeForSend());

                m.Id = toSend.Id;
            }
        }

        #endregion

        #region helper methods

		/// <summary>
		/// Waits for a message to become available on the input queue
		/// and then receives it.
		/// </summary>
		/// <remarks>
		/// If the queue is transactional the receive operation will be wrapped in a 
		/// transaction.
		/// </remarks>
        private void Receive()
        {
            if (this.distributorControlAddress != null)
                if (!sentReadyMessage)
                {
                    ReadyMessage rm = new ReadyMessage();

                    lock (readyMessageLocker)
                        if (readyMessageStartup)
                        {
                            rm.ClearPreviousFromThisAddress = true;
                            readyMessageStartup = false;
                        }

                    Msg ready = new Msg();
                    ready.Body = new IMessage[] { rm };
                    ready.ReturnAddress = this.Address;
                    ready.WindowsIdentityName = Thread.CurrentPrincipal.Identity.Name;

                    this.Send(ready, this.distributorControlAddress);
                    sentReadyMessage = true;
                }

            IAsyncResult ar = this.queue.BeginPeek();
            ar.AsyncWaitHandle.WaitOne();

            if (this.isTransactional)
                new TransactionWrapper().RunInTransaction(this.ReceiveFromQueue);
            else
                this.ReceiveFromQueue();
        }

		/// <summary>
		/// Receives a message from the input queue.
		/// </summary>
		/// <remarks>
		/// If a message is received the <see cref="MsgReceived"/> event will be raised.
		/// </remarks>
        public void ReceiveFromQueue()
        {
            try
            {
                Message m = this.queue.Receive(this.GetTransactionTypeForReceive());

                Msg result = Convert(m);

                try
                {
                    if (this.skipDeserialization)
                        result.BodyStream = m.BodyStream;
                    else
                        result.Body = Extract(m);
                }
                catch(Exception e)
                {
                    logger.Error("Could not deserialize message.", e);

                    if (this.errorQueue != null)
                        this.errorQueue.Send(m, this.GetTransactionTypeForSend());

                    return;
                }

                if (this.MsgReceived != null)
                    this.MsgReceived(this, new MsgReceivedEventArgs(result));
            }
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return;
            }

            sentReadyMessage = false;
            return;
        }

		/// <summary>
		/// Checks whether or not a queue is local by its path.
		/// </summary>
		/// <param name="value">The path to the queue to check.</param>
		/// <returns>true if the queue is local, otherwise false.</returns>
        public static bool QueueIsLocal(string value)
        {
            string machineName = Environment.MachineName.ToLower();

            value = value.Replace("FormatName:DIRECT=OS:", "");
            int index = value.IndexOf('\\');

            string queueMachineName = value.Substring(0, index).ToLower();

            return (machineName == queueMachineName || queueMachineName == "localhost" || queueMachineName == ".");
        }

		/// <summary>
		/// Converts an MSMQ <see cref="Message"/> into an NServiceBus message.
		/// </summary>
		/// <param name="m">The MSMQ message to convert.</param>
		/// <returns>An NServiceBus message.</returns>
        public static Msg Convert(Message m)
        {
            Msg result = new Msg();
            result.Id = m.Id;
            result.CorrelationId = (m.CorrelationId == "00000000-0000-0000-0000-000000000000\\0" ? null : m.CorrelationId);
            result.Recoverable = m.Recoverable;
            result.TimeToBeReceived = m.TimeToBeReceived;

            if (m.ResponseQueue != null)
                result.ReturnAddress = m.ResponseQueue.Path;

            result.WindowsIdentityName = m.Label;

            return result;
        }

		/// <summary>
		/// Resolves a destination MSMQ queue address.
		/// </summary>
		/// <param name="destination">The MSMQ address to resolve.</param>
		/// <returns>The direct format name of the queue.</returns>
        private static string Resolve(string destination)
        {
            string dest = destination.ToUpper().Replace("FORMATNAME:DIRECT=OS:", "");

            MessageQueue q = new MessageQueue(dest);
            if (QueueIsLocal(dest))
                q.MachineName = Environment.MachineName;

            return "FormatName:DIRECT=OS:" + q.Path;
        }

		/// <summary>
		/// Extracts the messages from an MSMQ <see cref="Message"/>.
		/// </summary>
		/// <param name="message">The MSMQ message to extract from.</param>
		/// <returns>An array of handleable messages.</returns>
        private IMessage[] Extract(Message message)
        {
            List<object> body;

            if (this.useXmlSerialization)
                body = this.xmlSerializer.Deserialize(message.BodyStream) as List<object>;
            else
                body = this.binaryFormatter.Deserialize(message.BodyStream) as List<object>;

            if (body == null)
                return null;

            IMessage[] result = new IMessage[body.Count];

            int i = 0;
            foreach (IMessage m in body)
                result[i++] = m;

            return result;
        }

		/// <summary>
		/// Gets the transaction type to use when receiving a message from the queue.
		/// </summary>
		/// <returns>The transaction type to use.</returns>
        private MessageQueueTransactionType GetTransactionTypeForReceive()
        {
            if (this.isTransactional)
                return MessageQueueTransactionType.Automatic;
            else
                return MessageQueueTransactionType.None;
        }

		/// <summary>
		/// Gets the transaction type to use when sending a message.
		/// </summary>
		/// <returns>The transaction type to use.</returns>
        private MessageQueueTransactionType GetTransactionTypeForSend()
        {
            if (this.isTransactional)
            {
                if (Transaction.Current != null)
                    return MessageQueueTransactionType.Automatic;
                else
                    return MessageQueueTransactionType.Single;
            }
            else
                return MessageQueueTransactionType.Single;
        }

		/// <summary>
		/// Sets the queue on the transport to the specified MSMQ queue.
		/// </summary>
		/// <param name="q">The MSMQ queue to set.</param>
        private void SetLocalQueue(MessageQueue q)
        {
            q.MachineName = Environment.MachineName; // just in case we were given "localhost"

            if (!q.Transactional)
                throw new ArgumentException("Queue must be transactional (" + q.Path + ").");
            else
                this.queue = q;

            MessagePropertyFilter mpf = new MessagePropertyFilter();
            mpf.SetAll();

            this.queue.MessageReadPropertyFilter = mpf;
        }

		/// <summary>
		/// Get a list of serializable types from the list of types provided.
		/// </summary>
		/// <param name="value">A list of types process.</param>
		/// <returns>A list of serializable types.</returns>
        private static Type[] GetExtraTypes(IEnumerable<Type> value)
        {
            List<Type> types = new List<Type>(value);
            foreach (Type t in value)
                if (CantSerializeType(t))
                    types.Remove(t);

            if (!types.Contains(typeof(List<object>)))
                types.Add(typeof(List<object>));

            return types.ToArray();
        }

		/// <summary>
		/// Indicates whether or not the specified type is serializable.
		/// </summary>
		/// <param name="t">The type to check.</param>
		/// <returns>true if the type is not serializable, otherwise false.</returns>
        private static bool CantSerializeType(Type t)
        {
            if (t.IsInterface || t.IsAbstract)
                return true;

            if (t.IsArray)
                return CantSerializeType(t.GetElementType());

            return false;
        }

        #endregion

        #region members

        private MessageQueue queue;
        private MessageQueue errorQueue;
        readonly IList<WorkerThread> workerThreads = new List<WorkerThread>();
        private XmlSerializer xmlSerializer = null;
        private BinaryFormatter binaryFormatter = new BinaryFormatter();

        /// <summary>
        /// ThreadStatic
        /// </summary>
        [ThreadStatic]
        static bool sentReadyMessage;

        static bool readyMessageStartup;
        static readonly object readyMessageLocker = new object();
        private static readonly ILog logger = LogManager.GetLogger(typeof (MsmqTransport));

        #endregion

        #region IDisposable Members

		/// <summary>
		/// Disposes the MSMQ queue of the transport.
		/// </summary>
        public void Dispose()
        {
            this.queue.Dispose();
        }

        #endregion
    }
}
