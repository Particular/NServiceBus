using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using NServiceBus.Utils;

namespace Timeout.MessageHandlers
{
    public class TimeoutPersister : IPersistTimeouts
    {
        public string Queue { get; set; }

        void IPersistTimeouts.Init()
        {
            MsmqUtilities.CreateQueueIfNecessary(Queue);

            var path = MsmqUtilities.GetFullPath(Queue);

            var mq = new MessageQueue(path);
            mq.MessageReadPropertyFilter.LookupId = true;

            if (!mq.Transactional)
                throw new Exception("Queue must be transactional.");

            storageQueue = mq;

            storageQueue.Formatter = new XmlMessageFormatter(new[] {typeof (TimeoutData)});

            lock(sagaToMessageLookup)
                storageQueue.GetAllMessages().ToList().ForEach(
                    m =>
                       {
                           var td = m.Body as TimeoutData;
                           if (td == null) //get rid of message
                               storageQueue.ReceiveById(m.Id, MessageQueueTransactionType.Single);
                           else //put into lookup
                               AddMessageToDictionary(td, m.Id);
                       });
        }

        IEnumerable<TimeoutData> IPersistTimeouts.GetAll()
        {
            return from m in storageQueue.GetAllMessages() where m.Body is TimeoutData select m.Body as TimeoutData;
        }

        void IPersistTimeouts.Add(TimeoutData timeout)
        {
            lock (sagaToMessageLookup)
            {
                var msg = new Message  { Body = timeout };

                storageQueue.Send(msg, MessageQueueTransactionType.Automatic);

                AddMessageToDictionary(timeout, msg.Id);
            }
        }

        /// <summary>
        /// Requires there to be a surrounding lock.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="lookupId"></param>
        private void AddMessageToDictionary(TimeoutData timeout, string messageId)
        {
            timeout.MessageId = messageId;

            if (!sagaToMessageLookup.ContainsKey(timeout.SagaId))
                sagaToMessageLookup[timeout.SagaId] = new List<TimeoutData>();

            sagaToMessageLookup[timeout.SagaId].Add(timeout);
        }

        bool IPersistTimeouts.Remove(TimeoutData timeout)
        {
            lock (sagaToMessageLookup)
            {
                if (!sagaToMessageLookup.ContainsKey(timeout.SagaId))
                    return false;

                var existing = sagaToMessageLookup[timeout.SagaId];

                TimeoutData selected = null;
                foreach (var td in existing.ToArray())
                    if (td.Time == timeout.Time)
                        selected = td;

                bool result = true;
                if (selected != null)
                {
                    try
                    {
                        storageQueue.ReceiveById(selected.MessageId, MessageQueueTransactionType.Automatic);
                    }
                    catch (InvalidOperationException) //msg ID not in queue
                    {
                        result = false;
                    }
                    finally
                    {
                        sagaToMessageLookup[timeout.SagaId].Remove(selected);
                    }
                }
                return result;
            }            
        }

        void IPersistTimeouts.ClearAll(Guid sagaId)
        {
            lock (sagaToMessageLookup)
                if (sagaToMessageLookup.ContainsKey(sagaId))
                {
                    var existing = sagaToMessageLookup[sagaId];
                    foreach (var td in existing)                        
                    {
                        try
                        {
                            storageQueue.ReceiveById(td.MessageId, MessageQueueTransactionType.Automatic);
                        }
                        catch (InvalidOperationException) //msg ID not in queue
                        { }
                    }

                    sagaToMessageLookup.Remove(sagaId);
                }
        }

        private MessageQueue storageQueue;
        private readonly Dictionary<Guid, List<TimeoutData>> sagaToMessageLookup = new Dictionary<Guid, List<TimeoutData>>();
    }
}
