namespace NServiceBus.Tools.Management.Errors.ReturnToSourceQueue
{
    using System;
    using System.Messaging;
    using System.Transactions;
    using Faults;
    using Transports.Msmq;

    public class ErrorManager
    {
        public bool ClusteredQueue { get; set; }

        public virtual Address InputQueue
        {
            set
            {
                var path = MsmqUtilities.GetFullPath(value);
                var q = new MessageQueue(path);

                if ((!ClusteredQueue) && (!q.Transactional))
                {
                    throw new ArgumentException(string.Format(NonTransactionalQueueErrorMessageFormat, q.Path));
                }

                queue = q;

                var messageReadPropertyFilter = new MessagePropertyFilter
                {
                    Body = true,
                    TimeToBeReceived = true,
                    Recoverable = true,
                    Id = true,
                    ResponseQueue = true,
                    CorrelationId = true,
                    Extension = true,
                    AppSpecific = true,
                    LookupId = true,
                };

                queue.MessageReadPropertyFilter = messageReadPropertyFilter;
            }
        }

        public void ReturnAll()
        {
            foreach (var m in queue.GetAllMessages())
            {
                ReturnMessageToSourceQueue(m.Id);
            }
        }

        /// <summary>
        ///     May throw a timeout exception if a message with the given id cannot be found.
        /// </summary>
        public void ReturnMessageToSourceQueue(string messageId)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    var message = queue.ReceiveById(messageId, TimeoutDuration, MessageQueueTransactionType.Automatic);

                    var tm = MsmqUtilities.Convert(message);
                    string failedQ = null;
                    if (tm.Headers.ContainsKey(FaultsHeaderKeys.FailedQ))
                    {
                        failedQ = tm.Headers[FaultsHeaderKeys.FailedQ];
                    }

                    if (string.IsNullOrEmpty(failedQ))
                    {
                        Console.WriteLine("ERROR: Message does not have a header indicating from which queue it came. Cannot be automatically returned to queue.");
                        return;
                    }

                    using (var q = new MessageQueue(MsmqUtilities.GetFullPath(Address.Parse(failedQ))))
                    {
                        q.Send(message, MessageQueueTransactionType.Automatic);
                    }

                    Console.WriteLine("Success.");
                    scope.Complete();
                }
                catch (MessageQueueException ex)
                {
                    if (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    {
                        Console.WriteLine(NoMessageFoundErrorFormat, messageId);

                        foreach (var m in queue.GetAllMessages())
                        {
                            var tm = MsmqUtilities.Convert(m);

                            string originalId = null;

                            if (tm.Headers.ContainsKey(Headers.MessageId))
                            {
                                originalId = tm.Headers[Headers.MessageId];
                            }

                            if (string.IsNullOrEmpty(originalId) && tm.Headers.ContainsKey(Headers.CorrelationId))
                            {
                                originalId = tm.Headers[Headers.CorrelationId];
                            }

                            if (string.IsNullOrEmpty(originalId) || messageId != originalId)
                            {
                                continue;
                            }

                            Console.WriteLine("Found message - going to return to queue.");

                            using (var tx = new TransactionScope())
                            {
                                using (var q = new MessageQueue(
                                    MsmqUtilities.GetFullPath(
                                        Address.Parse(tm.Headers[FaultsHeaderKeys.FailedQ]))))
                                {
                                    q.Send(m, MessageQueueTransactionType.Automatic);
                                }

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

        const string NonTransactionalQueueErrorMessageFormat = "Queue '{0}' must be transactional.";

        readonly string NoMessageFoundErrorFormat =
            string.Format("INFO: No message found with ID '{0}'. Going to check headers of all messages for one with '{0}' or '{1}'.", Headers.MessageId, Headers.CorrelationId);

        static readonly TimeSpan TimeoutDuration = TimeSpan.FromSeconds(5);
        MessageQueue queue;
    }
}