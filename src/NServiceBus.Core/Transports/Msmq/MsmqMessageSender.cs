namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;
    using System.Transactions;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports.Msmq.Config;
    using NServiceBus.Unicast.Queuing;

    /// <summary>
    /// Default MSMQ <see cref="ISendMessages"/> implementation.
    /// </summary>
    public class MsmqMessageSender : ISendMessages
    {
        readonly BehaviorContext context;

        /// <summary>
        /// Creates a new sender.
        /// </summary>
        /// <param name="context"></param>
        public MsmqMessageSender(BehaviorContext context)
        {
            Guard.AgainstNull(context, "context");
            this.context = context;
        }

        /// <summary>
        /// MsmqSettings
        /// </summary>
        public MsmqSettings Settings { get; set; }


        /// <summary>
        /// SuppressDistributedTransactions
        /// </summary>
        public bool SuppressDistributedTransactions { get; set; }

        /// <summary>
        /// Sends the given <paramref name="message"/>
        /// </summary>
        public void Send(OutgoingMessage message, TransportSendOptions sendOptions)
        {
            Guard.AgainstNull(message, "message");
            Guard.AgainstNull(sendOptions, "sendOptions");
            var destination = sendOptions.Destination;
            var destinationAddress = MsmqAddress.Parse(destination);
            var queuePath = MsmqUtilities.GetFullPath(destinationAddress);
            try
            {
                using (var q = new MessageQueue(queuePath, false, Settings.UseConnectionCache, QueueAccessMode.Send))
                using (var toSend = MsmqUtilities.Convert(message,sendOptions))
                {
                    toSend.UseDeadLetterQueue = Settings.UseDeadLetterQueue;
                    toSend.UseJournalQueue = Settings.UseJournalQueue;
                    toSend.TimeToReachQueue = Settings.TimeToReachQueue;

                    string replyToAddress;

                    if (message.Headers.TryGetValue(Headers.ReplyToAddress,out replyToAddress))
                    {
                        var returnAddress = MsmqUtilities.GetReturnAddress(replyToAddress, destinationAddress.Machine);
                        toSend.ResponseQueue = new MessageQueue(returnAddress);
                    }

                    MessageQueueTransaction receiveTransaction;
                    context.TryGet(out receiveTransaction);

                    if (sendOptions.EnlistInReceiveTransaction && receiveTransaction != null)
                    {
                        q.Send(toSend, receiveTransaction);
                    }
                    else
                    {
                        var transactionType = GetTransactionTypeForSend();

                        q.Send(toSend, transactionType);
                    }
                }
            }
            catch (MessageQueueException ex)
            {
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueNotFound)
                {
                    var msg = destination == null
                                     ? "Failed to send message. Target address is null."
                                     : string.Format("Failed to send message to address: [{0}]", destination);

                    throw new QueueNotFoundException(destination, msg, ex);
                }

                ThrowFailedToSendException(destination, ex);
            }
            catch (Exception ex)
            {
                ThrowFailedToSendException(destination, ex);
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

            return Transaction.Current != null
                       ? MessageQueueTransactionType.Automatic
                       : MessageQueueTransactionType.Single;
        }
    }
}