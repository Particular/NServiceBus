namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;
    using System.Transactions;
    using Config;
    using NServiceBus.Pipeline;
    using Settings;
    using Unicast.Queuing;

    /// <summary>
    ///     Msmq implementation of <see cref="ISendMessages" />.
    /// </summary>
    public class MsmqMessageSender : ISendMessages
    {
        /// <summary>
        ///     The current runtime settings for the transport
        /// </summary>
        public MsmqSettings Settings { get; set; }

        /// <summary>
        /// Msmq unit of work to be used in non DTC mode <see cref="MsmqUnitOfWork"/>.
        /// </summary>
        public MsmqUnitOfWork UnitOfWork { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }

        /// <summary>
        ///     Sends the given <paramref name="message" /> to the <paramref name="address" />.
        /// </summary>
        /// <param name="message">
        ///     <see cref="TransportMessage" /> to send.
        /// </param>
        /// <param name="address">
        ///     Destination <see cref="Address" />.
        /// </param>
        public void Send(TransportMessage message, Address address)
        {
            var queuePath = MsmqUtilities.GetFullPath(address);


            bool suppressNativeTransactions;
            PipelineExecutor.CurrentContext.TryGet("do-not-enlist-in-native-transaction", out suppressNativeTransactions);
            var transactionType = GetTransactionTypeForSend();
            if (message.TimeToBeReceived < MessageQueue.InfiniteTimeout && WillUseTransactionThatSupportsMultipleOperations(suppressNativeTransactions, transactionType))
            {
                throw new Exception($"Failed to send message to address: {address.Queue}@{address.Machine}. Sending messages with a custom TimeToBeReceived is not supported on transactional MSMQ.");
            }

            try
            {
                using (var q = new MessageQueue(queuePath, false, Settings.UseConnectionCache, QueueAccessMode.Send))
                {
                    using (var toSend = MsmqUtilities.Convert(message))
                    {
                        toSend.UseDeadLetterQueue = Settings.UseDeadLetterQueue;
                        toSend.UseJournalQueue = Settings.UseJournalQueue;

                        if (message.ReplyToAddress != null)
                            toSend.ResponseQueue =
                                new MessageQueue(MsmqUtilities.GetReturnAddress(message.ReplyToAddress.ToString(),
                                                                                address.ToString()));
                        if (UnitOfWork.HasActiveTransaction() && !suppressNativeTransactions)
                        {
                            q.Send(toSend, UnitOfWork.Transaction);
                        }
                        else
                        {
                            q.Send(toSend, transactionType);
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

        bool WillUseTransactionThatSupportsMultipleOperations(bool suppressNativeTransactions, MessageQueueTransactionType transactionType)
        {
            var willUseReceiveTransaction = UnitOfWork.HasActiveTransaction() && !suppressNativeTransactions;
            var willUseAutomaticTransaction = transactionType == MessageQueueTransactionType.Automatic;
            return willUseReceiveTransaction || willUseAutomaticTransaction;
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

            if (SettingsHolder.Get<bool>("Transactions.SuppressDistributedTransactions"))
            {
                return MessageQueueTransactionType.Single;
            }

            return Transaction.Current != null
                       ? MessageQueueTransactionType.Automatic
                       : MessageQueueTransactionType.Single;
        }
    }
}