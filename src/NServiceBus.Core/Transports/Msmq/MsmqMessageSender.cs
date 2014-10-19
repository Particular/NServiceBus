namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;
    using System.Transactions;
    using Config;
    using Unicast;
    using Unicast.Queuing;

    /// <summary>
    /// Default MSMQ <see cref="ISendMessages"/> implementation.
    /// </summary>
    public class MsmqMessageSender : ISendMessages
    {
        /// <summary>
        /// MsmqSettings
        /// </summary>
        public MsmqSettings Settings { get; set; }

        /// <summary>
        /// MsmqUnitOfWork
        /// </summary>
        public MsmqUnitOfWork UnitOfWork { get; set; }

        /// <summary>
        /// SuppressDistributedTransactions
        /// </summary>
        public bool SuppressDistributedTransactions { get; set; }

        /// <summary>
        /// Sends the given <paramref name="message"/>
        /// </summary>
        public void Send(TransportMessage message, SendOptions sendOptions)
        {
            var address = sendOptions.Destination;
            var queuePath = NServiceBus.MsmqUtilities.GetFullPath(address);
            try
            {
                using (var q = new MessageQueue(queuePath, false, Settings.UseConnectionCache, QueueAccessMode.Send))
                {
                    using (var toSend = NServiceBus.MsmqUtilities.Convert(message))
                    {
                        toSend.UseDeadLetterQueue = Settings.UseDeadLetterQueue;
                        toSend.UseJournalQueue = Settings.UseJournalQueue;
                        toSend.TimeToReachQueue = Settings.TimeToReachQueue;
                        
                        var replyToAddress = sendOptions.ReplyToAddress ?? message.ReplyToAddress;
                        
                        if (replyToAddress != null)
                        {
                            toSend.ResponseQueue = new MessageQueue(NServiceBus.MsmqUtilities.GetReturnAddress(replyToAddress.ToString(), address.ToString()));
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
                throw new Exception("Failed to send message.", ex);

            throw new Exception(
                string.Format("Failed to send message to address: {0}@{1}", address.Queue, address.Machine), ex);
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