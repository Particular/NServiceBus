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
            transactionTypeToUseForSend = useTransactionalQueue ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None;
            queue = new MessageQueue(queueAddress.FullPath);

            var messageReadPropertyFilter = new MessagePropertyFilter
            {
                Id = true,
                Body = true,
                Label = true,
                ArrivedTime = true
            };

            queue.Formatter = new XmlMessageFormatter(new[]
            {
                typeof(string)
            });

            queue.MessageReadPropertyFilter = messageReadPropertyFilter;
        }

        public IEnumerable<MsmqSubscriptionMessage> GetAllMessages()
        {
            return queue.GetAllMessages().Select(m => new MsmqSubscriptionMessage(m));
        }

        public string Send(string body, string label)
        {
            var toSend = new Message
            {
                Recoverable = true,
                Formatter = queue.Formatter,
                Body = body,
                Label = label
            };

            queue.Send(toSend, transactionTypeToUseForSend);

            return toSend.Id;
        }

        public void TryReceiveById(string messageId)
        {
            try
            {
                //Use of `None` here is intentional since ReceiveById works properly with this mode
                //for both transactional and non-transactional queues
                queue.ReceiveById(messageId, MessageQueueTransactionType.None);
            }
            catch (InvalidOperationException)
            {
                // thrown when message not found
            }
        }

        MessageQueueTransactionType transactionTypeToUseForSend;
        MessageQueue queue;
    }
}