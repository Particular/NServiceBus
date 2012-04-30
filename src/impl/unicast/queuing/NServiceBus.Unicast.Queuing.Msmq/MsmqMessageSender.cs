﻿using System;
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
            try
            {
                using (var q = new MessageQueue(queuePath, QueueAccessMode.Send))
                {
                    var toSend = MsmqUtilities.Convert(message);

                    toSend.UseDeadLetterQueue = UseDeadLetterQueue;
                    toSend.UseJournalQueue = UseJournalQueue;

                    if (message.ReplyToAddress != null)
                        toSend.ResponseQueue = new MessageQueue(MsmqUtilities.GetReturnAddress(message.ReplyToAddress.ToString(), address.ToString()));

                        q.Send(toSend, GetTransactionTypeForSend());

                    message.Id = toSend.Id;
                }
            }
            catch (MessageQueueException ex)
            {
                string msg = string.Empty;
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueNotFound)
                    if (address == null)
                        msg = "Failed to send message.";
                    else
                        msg = string.Format("Failed to send message to address: {0}@{1}", address.Queue, address.Machine);

                throw new QueueNotFoundException(address, msg, ex);
            }
            catch (Exception ex)
            {
                if(address == null)
                    throw new FailedToSendMessageException("Failed to send message.", ex);
                else
                    throw new FailedToSendMessageException(string.Format("Failed to send message to address: {0}@{1}", address.Queue, address.Machine),  ex);
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