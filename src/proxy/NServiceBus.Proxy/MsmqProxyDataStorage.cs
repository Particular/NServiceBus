using System;
using System.Messaging;
using NServiceBus.Utils;

namespace NServiceBus.Proxy
{
    class MsmqProxyDataStorage : IProxyDataStorage
    {
        private MessageQueue storageQueue;

        public string StorageQueue
        {
            get{ return s; }
            set
            {
                s = value;

                MsmqUtilities.CreateQueueIfNecessary(value);

                string path = MsmqUtilities.GetFullPath(value);

                var q = new MessageQueue(path);

                if (!q.Transactional)
                    throw new Exception("Queue must be transactional.");

                q.Formatter = new XmlMessageFormatter {TargetTypes = new[] {typeof (ProxyData)}};
                q.MessageReadPropertyFilter = new MessagePropertyFilter { Body = true, CorrelationId = true };

                storageQueue = q;

            }
        }
        private string s;

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
