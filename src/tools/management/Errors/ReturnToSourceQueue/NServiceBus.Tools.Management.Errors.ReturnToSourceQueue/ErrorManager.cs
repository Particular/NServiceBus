namespace NServiceBus.Tools.Management.Errors.ReturnToSourceQueue
{
    using System;
    using System.Messaging;
    using System.Transactions;
    using Utils;

    public class ErrorManager
    {
        private const string NonTransactionalQueueErrorMessageFormat = "Queue '{0}' must be transactional.";
        private const string NoMessageFoundErrorFormat = "INFO: No message found with ID '{0}'. Going to check headers of all messages for one with that original ID.";
        private MessageQueue queue;
        private static readonly TimeSpan TimeoutDuration = TimeSpan.FromSeconds(5);

        public virtual Address InputQueue
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
                try
                {
                    var message = queue.ReceiveById(messageId, TimeoutDuration, MessageQueueTransactionType.Automatic);

                    var tm = MsmqUtilities.Convert(message);

                    if (!tm.Headers.ContainsKey(Faults.HeaderKeys.FailedQ))
                    {
                        Console.WriteLine("ERROR: Message does not have a header indicating from which queue it came. Cannot be automatically returned to queue.");
                        return;
                    }

                    using (var q = new MessageQueue(MsmqUtilities.GetFullPath(Address.Parse(tm.Headers[Faults.HeaderKeys.FailedQ]))))
                        q.Send(message, MessageQueueTransactionType.Automatic);

                    Console.WriteLine("Success.");
                    scope.Complete();
                }
                catch(MessageQueueException ex)
                {
                    if (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    {
                        Console.WriteLine(NoMessageFoundErrorFormat, messageId);

                        foreach(var m in queue.GetAllMessages())
                        {
                            var tm = MsmqUtilities.Convert(m);

                            if (tm.Headers.ContainsKey(Faults.HeaderKeys.OriginalId))
                            {
                                if (messageId != tm.Headers[Faults.HeaderKeys.OriginalId])
                                    continue;

                                Console.WriteLine("Found message - going to return to queue.");

                                using (var tx = new TransactionScope(TransactionScopeOption.RequiresNew))
                                {
                                    using (var q = new MessageQueue(
                                                MsmqUtilities.GetFullPath(
                                                    Address.Parse(tm.Headers[Faults.HeaderKeys.FailedQ]))))
                                        q.Send(m, MessageQueueTransactionType.Automatic);

                                    queue.ReceiveByLookupId(MessageLookupAction.Current, m.LookupId,
                                                            MessageQueueTransactionType.Automatic);

                                    tx.Complete();
                                }

                                Console.WriteLine("Success.");
                                scope.Complete();

                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}
