using System;
using System.Messaging;
using NServiceBus.Unicast.Queuing.Msmq;

namespace NServiceBus.Proxy
{
    class MsmqProxyDataStorage : IProxyDataStorage
    {
        private MessageQueue storageQueue;

        public Address StorageQueue
        {
            get; set;
        }

        public void Init()
        {
            string path = MsmqUtilities.GetFullPath(StorageQueue);

            var q = new MessageQueue(path);

            if (!q.Transactional)
                throw new Exception("Queue must be transactional.");

            q.Formatter = new XmlMessageFormatter { TargetTypes = new[] { typeof(ProxyData) } };
            q.MessageReadPropertyFilter = new MessagePropertyFilter { Body = true, CorrelationId = true };

            storageQueue = q;
        }

        public void Save(ProxyData data)
        {
            storageQueue.Send(
                new Message {CorrelationId = data.Id, Body = data, Recoverable = true}, 
                MessageQueueTransactionType.Automatic);
        }

        public ProxyData GetAndRemove(string id)
        {
            try
            {
                var msg = storageQueue.ReceiveByCorrelationId(id);
                return msg.Body as ProxyData;
            }
            catch (Exception) // the receive method throws if the message isn't there
            {
                return null;
            }
        }
    }
}
