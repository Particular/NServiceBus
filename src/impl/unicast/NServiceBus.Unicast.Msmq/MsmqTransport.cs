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
    public class MsmqTransport : ITransport, IDisposable
    {
        #region config info

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

        public string ErrorQueue
        {
            set
            {
                this.errorQueue = new MessageQueue(value);
            }
        }

        private bool useXmlSerialization;
        public bool UseXmlSerialization
        {
            set { useXmlSerialization = value; }
        }

        private bool isTransactional;
        public bool IsTransactional
        {
            set { this.isTransactional = value; }
        }

        private bool skipDeserialization;
        public bool SkipDeserialization
        {
            set { skipDeserialization = value; }
        }

        private bool purgeOnStartup = false;
        public bool PurgeOnStartup
        {
            set { purgeOnStartup = value; }
        }

        private int numberOfWorkerThreads;
        public int NumberOfWorkerThreads
        {
            set { numberOfWorkerThreads = value; }
        }

        private string distributorControlAddress;
        public string DistributorControlAddress
        {
            set { distributorControlAddress = value; }
        }

        #endregion

        #region ITransport Members

        public event EventHandler<MsgReceivedEventArgs> MsgReceived;

        public string Address
        {
            get
            {
                return this.queue.Path;
            }
        }

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

        public void ReceiveMessageLater(Msg m)
        {
            this.Send(m, this.Address);
        }

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

        public static bool QueueIsLocal(string value)
        {
            string machineName = Environment.MachineName.ToLower();

            value = value.Replace("FormatName:DIRECT=OS:", "");
            int index = value.IndexOf('\\');

            string queueMachineName = value.Substring(0, index).ToLower();

            return (machineName == queueMachineName || queueMachineName == "localhost" || queueMachineName == ".");
        }

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

        private static string Resolve(string destination)
        {
            string dest = destination.ToUpper().Replace("FORMATNAME:DIRECT=OS:", "");

            MessageQueue q = new MessageQueue(dest);
            if (QueueIsLocal(dest))
                q.MachineName = Environment.MachineName;

            return "FormatName:DIRECT=OS:" + q.Path;
        }

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

        private MessageQueueTransactionType GetTransactionTypeForReceive()
        {
            if (this.isTransactional)
                return MessageQueueTransactionType.Automatic;
            else
                return MessageQueueTransactionType.None;
        }

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

        public void Dispose()
        {
            this.queue.Dispose();
        }

        #endregion
    }
}
