using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Transactions;
using System.Xml.Serialization;
using NServiceBus.Unicast.Transport;
using NServiceBus.Utils;

namespace NServiceBus.Unicast.Queuing.Msmq
{
    public class MsmqMessageSender : ISendMessages
    {
        void ISendMessages.Send(TransportMessage message, string destination)
        {
            ((ISendMessages)this).Send(message, Address.Parse(destination));
        }

        void ISendMessages.Send(TransportMessage message, Address address)
        {
            var queuePath = MsmqUtilities.GetFullPath(address);

            using (var q = new MessageQueue(queuePath, QueueAccessMode.Send))
            {
                var toSend = new Message();

                if (message.Body != null)
                    toSend.BodyStream = new MemoryStream(message.Body);

                if (message.CorrelationId != null)
                    toSend.CorrelationId = message.CorrelationId;

                toSend.Recoverable = message.Recoverable;
                toSend.UseDeadLetterQueue = UseDeadLetterQueue;
                toSend.UseJournalQueue = UseJournalQueue;

                if (message.ReplyToAddress != null)
                    toSend.ResponseQueue = new MessageQueue(MsmqUtilities.GetReturnAddress(message.ReplyToAddress.ToString(), address.ToString()));

                if (message.TimeToBeReceived < MessageQueue.InfiniteTimeout)
                    toSend.TimeToBeReceived = message.TimeToBeReceived;

                if (message.Headers == null)
                    message.Headers = new Dictionary<string, string>();

                if (!message.Headers.ContainsKey(HeaderKeys.IDFORCORRELATION))
                    message.Headers.Add(HeaderKeys.IDFORCORRELATION, null);

                if (String.IsNullOrEmpty(message.Headers[HeaderKeys.IDFORCORRELATION]))
                    message.Headers[HeaderKeys.IDFORCORRELATION] = message.IdForCorrelation;

                using (var stream = new MemoryStream())
                {
                    headerSerializer.Serialize(stream, message.Headers.Select(pair => new HeaderInfo { Key = pair.Key, Value = pair.Value }).ToList());
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
                        throw new QueueNotFoundException { Queue = address };

                    throw;
                }

                message.Id = toSend.Id;
            }
        }

        /// <summary>
        /// Determines if journaling should be activated
        /// </summary>
        public bool UseJournalQueue { get; set; }

        /// <summary>
        /// Determines if the dead letter queue should be used
        /// </summary>
        public bool UseDeadLetterQueue { get; set; }

        private static MessageQueueTransactionType GetTransactionTypeForSend()
        {
            return Transaction.Current != null ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.Single;
        }

        private readonly XmlSerializer headerSerializer = new XmlSerializer(typeof(List<HeaderInfo>));

    }
}