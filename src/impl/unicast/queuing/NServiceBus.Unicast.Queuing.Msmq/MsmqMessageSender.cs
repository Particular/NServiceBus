using System.Collections.Generic;
using System.Messaging;
using System.Transactions;
using System.Xml.Serialization;
using NServiceBus.Unicast.Transport;
using NServiceBus.Utils;

namespace NServiceBus.Unicast.Queuing.Msmq
{
    public class MsmqMessageSender : ISendMessages
    {
        void ISendMessages.Send(TransportMessage message, Address address)
        {
            var queuePath = MsmqUtilities.GetFullPath(address);

            using (var q = new MessageQueue(queuePath, QueueAccessMode.Send))
            {
                var toSend = MsmqUtilities.Convert(message);

                toSend.UseDeadLetterQueue = UseDeadLetterQueue;
                toSend.UseJournalQueue = UseJournalQueue;

                if (message.ReplyToAddress != null)
                    toSend.ResponseQueue = new MessageQueue(MsmqUtilities.GetReturnAddress(message.ReplyToAddress.ToString(), address.ToString()));

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