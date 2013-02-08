namespace NServiceBus.Unicast.Queuing.Msmq
{
    using System;
    using System.Messaging;
    using System.Transactions;
    using Config;

    public class MsmqMessageSender : ISendMessages
    {
        void ISendMessages.Send(TransportMessage message, Address address)
        {
            var queuePath = MsmqUtilities.GetFullPath(address);
            try
            {
                using (var q = new MessageQueue(queuePath, false, Settings.UseConnectionCache, QueueAccessMode.Send))
                {
                    using (Message toSend = MsmqUtilities.Convert(message))
                    {
                        toSend.UseDeadLetterQueue = Settings.UseDeadLetterQueue;
                        toSend.UseJournalQueue = Settings.UseJournalQueue;

                        if (message.ReplyToAddress != null)
                            toSend.ResponseQueue = new MessageQueue(MsmqUtilities.GetReturnAddress(message.ReplyToAddress.ToString(), address.ToString()));

                        q.Send(toSend, GetTransactionTypeForSend());
                    }
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
        /// The current runtime settings for the transport
        /// </summary>
        public MsmqSettings Settings{ get; set; }

        private static void ThrowFailedToSendException(Address address, Exception ex)
        {
            if (address == null)
                throw new FailedToSendMessageException("Failed to send message.", ex);

            throw new FailedToSendMessageException(
                string.Format("Failed to send message to address: {0}@{1}", address.Queue, address.Machine), ex);
        }

        MessageQueueTransactionType GetTransactionTypeForSend()
        {
            if (!Settings.UseTransactionalQueues)
            {
                return MessageQueueTransactionType.None;
            }

            if (Configure.Transactions.Advanced().SuppressDistributedTransactions)
            {
                return MessageQueueTransactionType.Single;
            }
            
            return Transaction.Current != null ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.Single;
        }
    }
}