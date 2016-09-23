namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Messaging;
    using System.Transactions;

    class MsmqSubscriptionStorageQueue : IMsmqSubscriptionStorageQueue
    {
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

        public IEnumerable<MsmqSubscriptionMessage> GetAllMessages()
        {
            return q.GetAllMessages().Select(m => new MsmqSubscriptionMessage(m));
        }

        public string Send(string body, string label)
        {
            var toSend = new Message()
            {
                Recoverable = true,
                Formatter = q.Formatter,
                Body = body,
                Label = label
            };

            q.Send(toSend, GetTransactionType(transactionsEnabled, dontUseExternalTransaction));

            return toSend.Id;
        }

        public void TryReceiveById(string messageId)
        {
            try
            {
                q.ReceiveById(messageId, GetTransactionType(transactionsEnabled, dontUseExternalTransaction));
            }
            catch (InvalidOperationException)
            {
                // thrown when message not found
            }
        }

        MessageQueueTransactionType GetTransactionType(bool transactionsEnabled, bool dontUseExternalTransaction)
        {
            if (!transactionsEnabled)
            {
                return MessageQueueTransactionType.None;
            }

            if (ConfigurationIsWrong(dontUseExternalTransaction))
            {
                throw new InvalidOperationException(@"This endpoint is not configured to be transactional. Processing subscriptions on a non-transactional endpoint is not supported by default. If this is still required, set the 'DontUseExternalTransaction' property of MsmqSubscriptionStorage to 'true'.
The recommended solution to this problem is to include '.IsTransaction(true)' after '.MsmqTransport()' in the initialization code, or if using NServiceBus.Host.exe to have the class which implements IConfigureThisEndpoint to also inherit AsA_Server or AsA_Publisher.");
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

        bool dontUseExternalTransaction;
        MessageQueue q;
        bool transactionsEnabled;
    }
}