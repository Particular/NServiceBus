using System;
using System.Collections.Generic;
using System.IO;
using System.Messaging;
using System.Transactions;
using System.Xml.Serialization;
using NServiceBus.Unicast.Transport;
using NServiceBus.Utils;

namespace NServiceBus.Unicast.Queuing.Msmq
{
    public class MsmqMessageQueue : IMessageQueue
    {
        public void Send(TransportMessage message, string destination)
        {
            var address = MsmqUtilities.GetFullPath(destination);

            using (var q = new MessageQueue(address, QueueAccessMode.Send))
            {
                var toSend = new Message();

                if (message.Body != null)
                    toSend.BodyStream = new MemoryStream(message.Body);

                if (message.CorrelationId != null)
                    toSend.CorrelationId = message.CorrelationId;

                toSend.Recoverable = message.Recoverable;

                if (!string.IsNullOrEmpty(message.ReturnAddress))
                    toSend.ResponseQueue = new MessageQueue(MsmqUtilities.GetFullPath(message.ReturnAddress));

                toSend.Label = GetLabel(message);

                if (message.TimeToBeReceived < MessageQueue.InfiniteTimeout)
                    toSend.TimeToBeReceived = message.TimeToBeReceived;

                if (message.Headers != null && message.Headers.Count > 0)
                {
                    using (var stream = new MemoryStream())
                    {
                        headerSerializer.Serialize(stream, message.Headers);
                        toSend.Extension = stream.GetBuffer();
                    }
                }

                toSend.AppSpecific = (int)message.MessageIntent;

                try
                {
                    q.Send(toSend, GetTransactionTypeForSend());
                }
                catch (MessageQueueException ex)
                {
                    if (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueNotFound)
                        throw new QueueNotFoundException {Queue = destination };

                    throw;
                }

                message.Id = toSend.Id;
            }
        }

        public void Init(string queue)
        {
            var machine = MsmqUtilities.GetMachineNameFromLogicalName(queue);

            if (machine.ToLower() != Environment.MachineName.ToLower())
                throw new InvalidOperationException("Input queue must be on the same machine as this process.");

            MsmqUtilities.CreateQueueIfNecessary(queue);

            myQueue = new MessageQueue(MsmqUtilities.GetFullPath(queue));

            bool transactional;
            try
            {
                transactional = myQueue.Transactional;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("There is a problem with the input queue given: {0}. See the enclosed exception for details.", queue), ex);
            }

            if (!transactional)
                throw new ArgumentException("Queue must be transactional (" + queue + ").");

            var mpf = new MessagePropertyFilter();
            mpf.SetAll();

            myQueue.MessageReadPropertyFilter = mpf;

            if (PurgeOnStartup)
                myQueue.Purge();
        }

        public bool HasMessage()
        {
            try
            {
                var m = myQueue.Peek(TimeSpan.FromSeconds(secondsToWait));
                return m != null;
            }
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return false;

                throw;
            }
        }

        public TransportMessage Receive(bool transactional)
        {
            try
            {
                var m = myQueue.Receive(TimeSpan.FromSeconds(secondsToWait), GetTransactionTypeForReceive(transactional));
                if (m == null)
                    return null;

                var result = new TransportMessage
                {
                    Id = m.Id,
                    CorrelationId =
                        (m.CorrelationId == "00000000-0000-0000-0000-000000000000\\0"
                             ? null
                             : m.CorrelationId),
                    Recoverable = m.Recoverable,
                    TimeToBeReceived = m.TimeToBeReceived,
                    TimeSent = m.SentTime,
                    ReturnAddress = MsmqUtilities.GetIndependentAddressForQueue(m.ResponseQueue),
                    MessageIntent = Enum.IsDefined(typeof(MessageIntentEnum), m.AppSpecific) ? (MessageIntentEnum)m.AppSpecific : MessageIntentEnum.Send
                };

                m.BodyStream.Position = 0;
                result.Body = new byte[m.BodyStream.Length];
                m.BodyStream.Read(result.Body, 0, result.Body.Length);

                FillIdForCorrelationAndWindowsIdentity(result, m);

                if (string.IsNullOrEmpty(result.IdForCorrelation))
                    result.IdForCorrelation = result.Id;

                if (m.Extension.Length > 0)
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
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return null;

                throw;
            }
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

        private static MessageQueueTransactionType GetTransactionTypeForReceive(bool transactional)
        {
            return transactional ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.None;
        }

        private static MessageQueueTransactionType GetTransactionTypeForSend()
        {
            return Transaction.Current != null ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.Single;
        }

        private static string GetLabel(TransportMessage m)
        {
            return string.Format("<{0}>{2}</{0}><{1}>{3}</{1}>", IDFORCORRELATION, WINDOWSIDENTITYNAME, m.IdForCorrelation, m.WindowsIdentityName);
        }

        private static void FillIdForCorrelationAndWindowsIdentity(TransportMessage result, Message m)
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

        /// <summary>
        /// Sets whether or not the transport should purge the input
        /// queue when it is started.
        /// </summary>
        public bool PurgeOnStartup { get; set; }


        private int secondsToWait = 1;
        public int SecondsToWaitForMessage
        {
            get { return secondsToWait;  }
            set { secondsToWait = value; }
        }

        private MessageQueue myQueue;

        private readonly XmlSerializer headerSerializer = new XmlSerializer(typeof(List<HeaderInfo>));

        private static readonly string IDFORCORRELATION = "CorrId";
        private static readonly string WINDOWSIDENTITYNAME = "WinIdName";
        private static readonly string FAILEDQUEUE = "FailedQ";
    }
}
