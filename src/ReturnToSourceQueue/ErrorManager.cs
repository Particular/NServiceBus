namespace NServiceBus.Tools.Management.Errors.ReturnToSourceQueue
{
    using System;
    using System.Messaging;
    using System.Transactions;
    using Faults;

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
                    throw new ArgumentException(string.Format("Queue '{0}' must be transactional.", q.Path));
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
        ///   May throw a timeout exception if a message with the given id cannot be found.
        /// </summary>
        public void ReturnMessageToSourceQueue(string messageId)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    var message = queue.ReceiveById(messageId, TimeoutDuration, MessageQueueTransactionType.Automatic);

                    var tm = MsmqUtilities.Convert(message);
                    string failedQ;
                    if (!tm.Headers.TryGetValue(FaultsHeaderKeys.FailedQ, out failedQ))
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

                        uint messageCount = 0;
                        foreach (var m in queue.GetAllMessages())
                        {
                            messageCount++;
                            var tm = MsmqUtilities.Convert(m);

                            var originalId = GetOriginalId(tm);

                            if (string.IsNullOrEmpty(originalId) || messageId != originalId)
                            {
                                if (messageCount % ProgressInterval == 0)
                                {
                                    Console.Write(".");
                                }
                                continue;
                            }

                            Console.WriteLine();
                            Console.WriteLine("Found message - going to return to queue.");

                            using (var tx = new TransactionScope())
                            {
                                var failedQueue = tm.Headers[FaultsHeaderKeys.FailedQ];
                                using (var q = new MessageQueue(MsmqUtilities.GetFullPath(Address.Parse(failedQueue))))
                                {
                                    q.Send(m, MessageQueueTransactionType.Automatic);
                                }

                                queue.ReceiveByLookupId(MessageLookupAction.Current, m.LookupId, MessageQueueTransactionType.Automatic);

                                tx.Complete();
                            }

                            Console.WriteLine("Success.");
                            scope.Complete();

                            return;
                        }

                        Console.WriteLine();
                        Console.WriteLine(NoMessageFoundInHeadersErrorFormat, messageId);
                    }
                }
            }
        }

        string GetOriginalId(TransportMessage tm)
        {
            string originalId;

            if (tm.Headers.TryGetValue("NServiceBus.OriginalId", out originalId))
            {
                return originalId;
            }
            if (tm.Headers.TryGetValue(Headers.MessageId, out originalId))
            {
                return originalId;
            }

            return null;
        }


        const string NoMessageFoundErrorFormat = "INFO: No message found with ID '{0}'. Checking headers of all messages.";
        const string NoMessageFoundInHeadersErrorFormat = "INFO: No message found with ID '{0}' in any headers.";
        const uint ProgressInterval = 100;

        TimeSpan TimeoutDuration = TimeSpan.FromSeconds(5);
        MessageQueue queue;

    }
}