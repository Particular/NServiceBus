namespace NServiceBus.Tools.Management.Errors.ReturnToSourceQueue
{
    using System;
    using System.Messaging;
    using System.Transactions;
    using Utils;

    public class ErrorManager
    {
        private const string NonTransactionalQueueErrorMessageFormat = "Queue '{0}' must be transactional.";
        private const string NoMessageFoundErrorFormat = "No message found with ID '{0}'.";
        private MessageQueue queue;
        private static readonly TimeSpan TimeoutDuration = TimeSpan.FromSeconds(5);

        public virtual string InputQueue
        {
            set
            {
                var path = MsmqUtilities.GetFullPath(value);
                var q = new MessageQueue(path);

                if (!q.Transactional)
                    throw new ArgumentException(string.Format(NonTransactionalQueueErrorMessageFormat, q.Path));

                queue = q;

                var mpf = new MessagePropertyFilter();
                mpf.SetAll();

                queue.MessageReadPropertyFilter = mpf;
            }
        }

        public void ReturnAll()
        {
            foreach (var m in queue.GetAllMessages())
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
                var message = queue.ReceiveById(messageId, TimeoutDuration, MessageQueueTransactionType.Automatic);

                if (message == null)
                    Console.WriteLine(NoMessageFoundErrorFormat, messageId);
                else
                    using (var q = message.ResponseQueue)
                        q.Send(message, MessageQueueTransactionType.Automatic);

                scope.Complete();
            }
        }
    }
}
