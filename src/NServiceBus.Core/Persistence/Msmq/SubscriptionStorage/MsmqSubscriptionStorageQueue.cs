namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Transactions;

    class MsmqSubscriptionStorageQueue : IMsmqSubscriptionStorageQueue
    {
        bool transactionsEnabled;
        bool dontUseExternalTransaction;
        MessageQueue q;

        public MsmqSubscriptionStorageQueue(MsmqAddress queueAddress, bool transactionsEnabled, bool dontUseExternalTransaction)
        {
            this.transactionsEnabled = transactionsEnabled;
            this.dontUseExternalTransaction = dontUseExternalTransaction;
            q = new MessageQueue(queueAddress.FullPath);
            bool transactional;
            try
            {
                transactional = q.Transactional;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"There is a problem with the subscription storage queue {queueAddress}. See enclosed exception for details.", ex);
            }

            if (!transactional && transactionsEnabled)
            {
                throw new ArgumentException("Queue must be transactional (" + queueAddress + ").");
            }

            var messageReadPropertyFilter = new MessagePropertyFilter
            {
                Id = true,
                Body = true,
                Label = true
            };

            q.Formatter = new XmlMessageFormatter(new[]
            {
                typeof(string)
            });

            q.MessageReadPropertyFilter = messageReadPropertyFilter;            
        }

        MessageQueueTransactionType GetTransactionType(bool transactionsEnabled, bool dontUseExternalTransaction)
        {
            if (!transactionsEnabled)
            {
                return MessageQueueTransactionType.None;
            }

            if (ConfigurationIsWrong(dontUseExternalTransaction))
            {
                throw new InvalidOperationException("This endpoint is not configured to be transactional. Processing subscriptions on a non-transactional endpoint is not supported by default. If you still wish to do so, please set the 'DontUseExternalTransaction' property of MsmqSubscriptionStorage to 'true'.\n\nThe recommended solution to this problem is to include '.IsTransaction(true)' after '.MsmqTransport()' in your fluent initialization code, or if you're using NServiceBus.Host.exe to have the class which implements IConfigureThisEndpoint to also inherit AsA_Server or AsA_Publisher.");
            }

            var t = MessageQueueTransactionType.Automatic;
            if (dontUseExternalTransaction)
            {
                t = MessageQueueTransactionType.Single;
            }

            return t;
        }

        bool ConfigurationIsWrong(bool DontUseExternalTransaction)
        {
            return Transaction.Current == null && !DontUseExternalTransaction;
        }

        public IEnumerable<Message> GetAllMessages()
        {
            return q.GetAllMessages();
        }

        public void Send(Message toSend)
        {
            toSend.Formatter = q.Formatter;
            q.Send(toSend, GetTransactionType(transactionsEnabled, dontUseExternalTransaction));
        }

        public void ReceiveById(string messageId)
        {
            q.ReceiveById(messageId, GetTransactionType(transactionsEnabled, dontUseExternalTransaction));
        }
    }
}