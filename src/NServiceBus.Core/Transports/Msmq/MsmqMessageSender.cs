namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;
    using System.Transactions;
    using Config;
    using Settings;
    using Unicast;
    using Unicast.Queuing;

    class MsmqMessageSender : ISendMessages
    {
        public MsmqSettings Settings { get; set; }

        public MsmqUnitOfWork UnitOfWork { get; set; }



        public void Send(TransportMessage message, SendOptions sendOptions)
        {
            var address = sendOptions.Destination;
            var queuePath = MsmqUtilities.GetFullPath(address);
            try
            {
                using (var q = new MessageQueue(queuePath, false, Settings.UseConnectionCache, QueueAccessMode.Send))
                {
                    using (var toSend = MsmqUtilities.Convert(message))
                    {
                        toSend.UseDeadLetterQueue = Settings.UseDeadLetterQueue;
                        toSend.UseJournalQueue = Settings.UseJournalQueue;

                        var replyToAddress = sendOptions.ReplyToAddress ?? message.ReplyToAddress;
                        
                        if (replyToAddress != null)
                        {
                            toSend.ResponseQueue = new MessageQueue(MsmqUtilities.GetReturnAddress(replyToAddress.ToString(), address.ToString()));
                        }


                        if (sendOptions.EnlistInReceiveTransaction && UnitOfWork.HasActiveTransaction())
                        {
                            q.Send(toSend, UnitOfWork.Transaction);
                        }
                        else
                        {
                            q.Send(toSend, GetTransactionTypeForSend());
                        }
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

        static void ThrowFailedToSendException(Address address, Exception ex)
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

            if (SettingsHolder.Instance.Get<bool>("Transactions.SuppressDistributedTransactions"))
            {
                return MessageQueueTransactionType.Single;
            }

            return Transaction.Current != null
                       ? MessageQueueTransactionType.Automatic
                       : MessageQueueTransactionType.Single;
        }
    }
}