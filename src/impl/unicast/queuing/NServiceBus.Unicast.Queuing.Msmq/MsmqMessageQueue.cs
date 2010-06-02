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

                if (message.TimeToBeReceived < MessageQueue.InfiniteTimeout)
                    toSend.TimeToBeReceived = message.TimeToBeReceived;

                if (message.Headers == null)
                    message.Headers = new List<HeaderInfo>();

                message.Headers.Add(new HeaderInfo {Key = IDFORCORRELATION, Value = message.IdForCorrelation});

                using (var stream = new MemoryStream())
                {
                    headerSerializer.Serialize(stream, message.Headers);
                    toSend.Extension = stream.GetBuffer();
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

                result.IdForCorrelation = GetIdForCorrelation(result.Headers);
                if (result.IdForCorrelation == null)
                    result.IdForCorrelation = result.Id;

                return result;
            }
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return null;

                throw;
            }
        }

        private static string GetIdForCorrelation(List<HeaderInfo> list)
        {
            var header = list.Find(hi => hi.Key == IDFORCORRELATION);
            return header != null ? header.Value : null;
        }

        private static MessageQueueTransactionType GetTransactionTypeForReceive(bool transactional)
        {
            return transactional ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.None;
        }

        private static MessageQueueTransactionType GetTransactionTypeForSend()
        {
            return Transaction.Current != null ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.Single;
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
        private const string IDFORCORRELATION = "CorrId";
        private readonly XmlSerializer headerSerializer = new XmlSerializer(typeof(List<HeaderInfo>));
    }
}
