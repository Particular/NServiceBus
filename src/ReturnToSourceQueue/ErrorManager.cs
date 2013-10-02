namespace NServiceBus.Tools.Management.Errors.ReturnToSourceQueue
{
    using System;
    using System.Messaging;
    using System.Transactions;
    using Transports.Msmq;

    public class ErrorManager
    {
        private const string NonTransactionalQueueErrorMessageFormat = "Queue '{0}' must be transactional.";
        private const string NoMessageFoundErrorFormat = "INFO: No message found with ID '{0}'. Going to check headers of all messages for one with that original ID.";
        private MessageQueue queue;
        private static readonly TimeSpan TimeoutDuration = TimeSpan.FromSeconds(5);
        public bool ClusteredQueue { get; set; }
        /// <summary>
        /// Constant taken from V2.6: 
        /// https://github.com/NServiceBus/NServiceBus/blob/v2.5/src/impl/unicast/NServiceBus.Unicast.Msmq/MsmqTransport.cs
        /// </summary>
        const string FailedQueue = "FailedQ";

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
                    string failedQ = null;
                    if (tm.Headers.ContainsKey(Faults.FaultsHeaderKeys.FailedQ))
                    {
                        failedQ = tm.Headers[Faults.FaultsHeaderKeys.FailedQ];
                    }
                    
                    if (string.IsNullOrEmpty(failedQ))
                    {
                        Console.WriteLine("ERROR: Message does not have a header (or label) indicating from which queue it came. Cannot be automatically returned to queue.");
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

                            if (tm.Headers.ContainsKey("NServiceBus.OriginalId"))
                                originalId = tm.Headers["NServiceBus.OriginalId"];


                            if (string.IsNullOrEmpty(originalId) && tm.Headers.ContainsKey("CorrId"))
                                originalId = tm.Headers["CorrId"];

                            if (string.IsNullOrEmpty(originalId) || messageId != originalId)
                                continue;


                            Console.WriteLine("Found message - going to return to queue.");

                            using (var tx = new TransactionScope())
                            {
                                using (var q = new MessageQueue(
                                            MsmqUtilities.GetFullPath(
                                                Address.Parse(tm.Headers[Faults.FaultsHeaderKeys.FailedQ]))))
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
