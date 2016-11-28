namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Messaging;

    class MsmqSubscriptionStorageQueue : IMsmqSubscriptionStorageQueue
    {
        public MsmqSubscriptionStorageQueue(MsmqAddress queueAddress, bool useTransactionalQueue)
        {
            transactionTypeToUse = useTransactionalQueue ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None;
            q = new MessageQueue(queueAddress.FullPath);

            var messageReadPropertyFilter = new MessagePropertyFilter
            {
                Id = true,
                Body = true,
                Label = true,
                ArrivedTime = true
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
            var toSend = new Message
            {
                Recoverable = true,
                Formatter = q.Formatter,
                Body = body,
                Label = label
            };

            q.Send(toSend, transactionTypeToUse);

            return toSend.Id;
        }

        public void TryReceiveById(string messageId)
        {
            try
            {
                //todo: add failing test before fixing this
                q.ReceiveById(messageId, MessageQueueTransactionType.None);
            }
            catch (InvalidOperationException)
            {
                // thrown when message not found
            }
        }

        MessageQueueTransactionType transactionTypeToUse;
        MessageQueue q;
    }
}