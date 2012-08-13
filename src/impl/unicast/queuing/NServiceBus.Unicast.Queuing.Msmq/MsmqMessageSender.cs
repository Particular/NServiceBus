namespace NServiceBus.Unicast.Queuing.Msmq
{
    using System;
    using System.Messaging;
    using System.Transactions;
    using Utils;
    using Config;

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
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueNotFound)
                {
                    var msg = address == null
                                     ? "Failed to send message. Target address is null."
                                     : string.Format("Failed to send message to address: [{0}]", address);

                    throw new QueueNotFoundException(address, msg, ex);
                }

                ThrowFailedToSendException(address, ex);
            }
            catch (Exception ex)
            {
                ThrowFailedToSendException(address, ex);
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

        private static void ThrowFailedToSendException(Address address, Exception ex)
        {
            if (address == null)
                throw new FailedToSendMessageException("Failed to send message.", ex);

            throw new FailedToSendMessageException(
                string.Format("Failed to send message to address: {0}@{1}", address.Queue, address.Machine), ex);
        }

        private static MessageQueueTransactionType GetTransactionTypeForSend()
        {
            if(Endpoint.IsVolatile)
                return MessageQueueTransactionType.None;
            
            return Transaction.Current != null ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.Single;
        }
    }
}