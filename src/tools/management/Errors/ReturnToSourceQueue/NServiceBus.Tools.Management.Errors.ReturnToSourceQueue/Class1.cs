using System;
using System.Messaging;
using System.Transactions;
using NServiceBus.Utils;

namespace NServiceBus.Tools.Management.Errors.ReturnToSourceQueue
{
    public class ErrorManager
    {
        private MessageQueue queue;

        public virtual string InputQueue
        {
            set
            {
                string path = MsmqUtilities.GetFullPath(value);
                var q = new MessageQueue(path);

                if (!q.Transactional)
                    throw new ArgumentException("Queue must be transactional (" + q.Path + ").");

                queue = q;

                var mpf = new MessagePropertyFilter();
                mpf.SetAll();

                queue.MessageReadPropertyFilter = mpf;
            }
        }

        public void ReturnAll()
        {
            foreach(var m in queue.GetAllMessages())
                ReturnMessageToSourceQueue(m.Id);
        }

        /// <summary>
        /// May throw a timeout exception if a message with the given id cannot be found.
        /// </summary>
        /// <param name="messageId"></param>
        public void ReturnMessageToSourceQueue(string messageId)
        {
            using (var scope = new TransactionScope())
            {
                var m = queue.ReceiveById(messageId, TimeSpan.FromSeconds(5), MessageQueueTransactionType.Automatic);

                using (var q = m.ResponseQueue)
                {
                    q.Send(m, MessageQueueTransactionType.Automatic);
                }
                
                scope.Complete();
            }
        }
    }
}
