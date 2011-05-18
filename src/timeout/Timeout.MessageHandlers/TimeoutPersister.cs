using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using NServiceBus;
using NServiceBus.Utils;

namespace Timeout.MessageHandlers
{
    public class TimeoutPersister : IPersistTimeouts
    {
        public Address Queue
        {
            get { return q; } 
            set { q = value; Init(value); }
        }
        private Address q;

        private void Init(Address queue)
        {
            var path = MsmqUtilities.GetFullPath(queue);

            var mq = new MessageQueue(path);

            if (!mq.Transactional)
                throw new Exception("Queue must be transactional.");

            storageQueue = mq;

            storageQueue.Formatter = new XmlMessageFormatter(new[] {typeof (TimeoutData)});

            storageQueue.GetAllMessages().ToList().ForEach(
                m =>
                   {
                       var td = m.Body as TimeoutData;
                       if (td == null) //get rid of message
                           storageQueue.ReceiveById(m.Id, MessageQueueTransactionType.Single);
                       else //put into lookup
                           sagaToMessageIdLookup[td.SagaId] = m.Id;
                   });
        }

        IEnumerable<TimeoutData> IPersistTimeouts.GetAll()
        {
            return from m in storageQueue.GetAllMessages() where m.Body is TimeoutData select m.Body as TimeoutData;
        }

        void IPersistTimeouts.Add(TimeoutData timeout)
        {
            var msg = new Message
                        {
                            Body = timeout,
                            Label = timeout.SagaId.ToString()
                        };

            storageQueue.Send(msg, MessageQueueTransactionType.Automatic);

            lock (sagaToMessageIdLookup)
                sagaToMessageIdLookup[timeout.SagaId] = msg.Id;
        }

        void IPersistTimeouts.Remove(Guid sagaId)
        {
            lock (sagaToMessageIdLookup)
                if (sagaToMessageIdLookup.ContainsKey(sagaId))
                {
                    var msgId = sagaToMessageIdLookup[sagaId];

                    storageQueue.ReceiveById(msgId, MessageQueueTransactionType.Automatic); 
                    sagaToMessageIdLookup.Remove(sagaId);
                }
        }

        private MessageQueue storageQueue;
        private readonly Dictionary<Guid, string> sagaToMessageIdLookup = new Dictionary<Guid, string>();
    }
}
