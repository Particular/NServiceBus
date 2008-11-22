using System;
using System.Messaging;
using System.Transactions;
using NServiceBus.Unicast.Transport.Msmq;

namespace NServiceBus.Tools.Management.Errors.ReturnToSourceQueue
{
    public class Class1
    {
        private MessageQueue queue;

        public virtual string InputQueue
        {
            set
            {
                string path = MsmqTransport.GetFullPath(value);
                MessageQueue q = new MessageQueue(path);

                if (!q.Transactional)
                    throw new ArgumentException("Queue must be transactional (" + q.Path + ").");

                this.queue = q;

                MessagePropertyFilter mpf = new MessagePropertyFilter();
                mpf.SetAll();

                this.queue.MessageReadPropertyFilter = mpf;
            }
        }

        public void ReturnAll()
        {
            foreach(Message m in queue.GetAllMessages())
                ReturnMessageToSourceQueue(m.Id);
        }

        /// <summary>
        /// May throw a timeout exception if a message with the given id cannot be found.
        /// </summary>
        /// <param name="messageId"></param>
        public void ReturnMessageToSourceQueue(string messageId)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                Message m = this.queue.ReceiveById(messageId, TimeSpan.FromSeconds(5), MessageQueueTransactionType.Automatic);

                string failedQueue = MsmqTransport.GetFailedQueue(m);

                m.Label = MsmqTransport.GetLabelWithoutFailedQueue(m);

                using (MessageQueue q = new MessageQueue(failedQueue))
                {
                    q.Send(m, MessageQueueTransactionType.Automatic);
                }
                
                scope.Complete();
            }
        }
    }
}
