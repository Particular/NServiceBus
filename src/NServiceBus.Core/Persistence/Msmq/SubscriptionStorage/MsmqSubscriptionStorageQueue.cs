namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Messaging;

    class MsmqSubscriptionStorageQueue : IMsmqSubscriptionStorageQueue
    {
        public MsmqSubscriptionStorageQueue(MsmqAddress queueAddress)
        {
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
            var toSend = new Message()
            {
                Recoverable = true,
                Formatter = q.Formatter,
                Body = body,
                Label = label
            };

            q.Send(toSend, MessageQueueTransactionType.None);

            return toSend.Id;
        }

        public void TryReceiveById(string messageId)
        {
            try
            {
                q.ReceiveById(messageId, MessageQueueTransactionType.None);
            }
            catch (InvalidOperationException)
            {
                // thrown when message not found
            }
        }

        MessageQueue q;
    }
}