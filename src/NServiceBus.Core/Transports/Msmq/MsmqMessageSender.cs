namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;
    using System.Threading;
    using System.Transactions;
    using Config;
    using Settings;
    using Unicast.Queuing;

    /// <summary>
    ///     Msmq implementation of <see cref="ISendMessages" />.
    /// </summary>
    public class MsmqMessageSender : ISendMessages, IDisposable
    {
        private readonly ThreadLocal<MessageQueueTransaction> currentTransaction =
            new ThreadLocal<MessageQueueTransaction>();

        private bool disposed;

        /// <summary>
        ///     The current runtime settings for the transport
        /// </summary>
        public MsmqSettings Settings { get; set; }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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
            string queuePath = MsmqUtilities.GetFullPath(address);
            try
            {
                using (var q = new MessageQueue(queuePath, false, Settings.UseConnectionCache, QueueAccessMode.Send))
                {
                    using (Message toSend = MsmqUtilities.Convert(message))
                    {
                        toSend.UseDeadLetterQueue = Settings.UseDeadLetterQueue;
                        toSend.UseJournalQueue = Settings.UseJournalQueue;

                        if (message.ReplyToAddress != null)
                            toSend.ResponseQueue =
                                new MessageQueue(MsmqUtilities.GetReturnAddress(message.ReplyToAddress.ToString(),
                                                                                address.ToString()));

                        if (currentTransaction.IsValueCreated)
                        {
                            q.Send(toSend, currentTransaction.Value);
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
                    string msg = address == null
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
        ///     Sets the native transaction.
        /// </summary>
        /// <param name="transaction">
        ///     Native <see cref="MessageQueueTransaction" />.
        /// </param>
        public void SetTransaction(MessageQueueTransaction transaction)
        {
            currentTransaction.Value = transaction;
        }

        private static void ThrowFailedToSendException(Address address, Exception ex)
        {
            if (address == null)
                throw new FailedToSendMessageException("Failed to send message.", ex);

            throw new FailedToSendMessageException(
                string.Format("Failed to send message to address: {0}@{1}", address.Queue, address.Machine), ex);
        }

        private MessageQueueTransactionType GetTransactionTypeForSend()
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

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed resources.
                currentTransaction.Dispose();
            }

            disposed = true;
        }

        ~MsmqMessageSender()
        {
            Dispose(false);
        }
    }
}