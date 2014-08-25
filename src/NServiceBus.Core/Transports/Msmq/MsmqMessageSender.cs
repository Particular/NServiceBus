namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;
    using System.Transactions;
    using Config;
    using Unicast;
    using Unicast.Queuing;

    class MsmqMessageSender : ISendMessages
    {
        public MsmqSettings Settings { get; set; }

        public MsmqUnitOfWork UnitOfWork { get; set; }

        public bool SuppressDistributedTransactions { get; set; }

        public void Send(TransportMessage message, SendOptions sendOptions)
        {
            var parsedAddress = Address.Parse(sendOptions.Destination);
            var queuePath = NServiceBus.MsmqUtilities.GetFullPath(parsedAddress);
            try
            {
                using (var q = new MessageQueue(queuePath, false, Settings.UseConnectionCache, QueueAccessMode.Send))
                {
                    using (var toSend = NServiceBus.MsmqUtilities.Convert(message))
                    {
                        toSend.UseDeadLetterQueue = Settings.UseDeadLetterQueue;
                        toSend.UseJournalQueue = Settings.UseJournalQueue;

                        var replyToAddress = sendOptions.ReplyToAddress ?? message.ReplyToAddress;
                        
                        if (replyToAddress != null)
                        {
                            toSend.ResponseQueue = new MessageQueue(NServiceBus.MsmqUtilities.GetReturnAddress(replyToAddress, sendOptions.Destination));
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
                    var msg = sendOptions.Destination == null
                                     ? "Failed to send message. Target address is null."
                                     : string.Format("Failed to send message to address: [{0}]", sendOptions.Destination);

                    throw new QueueNotFoundException(sendOptions.Destination, msg, ex);
                }

                ThrowFailedToSendException(sendOptions.Destination, ex);
            }
            catch (Exception ex)
            {
                ThrowFailedToSendException(sendOptions.Destination, ex);
            }
        }

        static void ThrowFailedToSendException(string address, Exception ex)
        {
            if (address == null)
                throw new Exception("Failed to send message.", ex);

            throw new Exception(
                string.Format("Failed to send message to address: {0}", address), ex);
        }

        MessageQueueTransactionType GetTransactionTypeForSend()
        {
            if (!Settings.UseTransactionalQueues)
            {
                return MessageQueueTransactionType.None;
            }

            if (SuppressDistributedTransactions)
            {
                return MessageQueueTransactionType.Single;
            }

            return Transaction.Current != null
                       ? MessageQueueTransactionType.Automatic
                       : MessageQueueTransactionType.Single;
        }
    }
}