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
        private const string FAILEDQUEUE = "FailedQ";

        public virtual Address InputQueue
        {
            set
            {
                var path = MsmqUtilities.GetFullPath(value);
                var q = new MessageQueue(path);

                if ((!ClusteredQueue) && (!q.Transactional))
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
                    string failedQ;
                    if (tm.Headers.ContainsKey(Faults.FaultsHeaderKeys.FailedQ))
                        failedQ = tm.Headers[Faults.FaultsHeaderKeys.FailedQ];
                    else // try to bring failedQ from label, v2.6 style.
                    {
                        failedQ = GetFailedQueueFromLabel(message);
                        if (!string.IsNullOrEmpty(failedQ))
                            message.Label = GetLabelWithoutFailedQueue(message);
                    }

                    if (string.IsNullOrEmpty(failedQ))
                    {
                        Console.WriteLine("ERROR: Message does not have a header (or label) indicating from which queue it came. Cannot be automatically returned to queue.");
                        return;
                    }

                    using (var q = new MessageQueue(MsmqUtilities.GetFullPath(Address.Parse(failedQ))))
                        q.Send(message, MessageQueueTransactionType.Automatic);

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

        /// <summary>
        /// For compatibility with V2.6:
        /// Gets the label of the message stripping out the failed queue.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static string GetLabelWithoutFailedQueue(Message m)
        {
            if (string.IsNullOrEmpty(m.Label))
                return string.Empty;

            if (!m.Label.Contains(FAILEDQUEUE))
                return m.Label;

            var startIndex = m.Label.IndexOf(string.Format("<{0}>", FAILEDQUEUE));
            var endIndex = m.Label.IndexOf(string.Format("</{0}>", FAILEDQUEUE));
            endIndex += FAILEDQUEUE.Length + 3;

            return m.Label.Remove(startIndex, endIndex - startIndex);
        }
        /// <summary>
        /// For compatibility with V2.6:
        /// Returns the queue whose process failed processing the given message
        /// by accessing the label of the message.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static string GetFailedQueueFromLabel(Message m)
        {
            if (m.Label == null)
                return null;

            if (!m.Label.Contains(FAILEDQUEUE))
                return null;

            var startIndex = m.Label.IndexOf(string.Format("<{0}>", FAILEDQUEUE)) + FAILEDQUEUE.Length + 2;
            var count = m.Label.IndexOf(string.Format("</{0}>", FAILEDQUEUE)) - startIndex;

            return m.Label.Substring(startIndex, count);
        }
    }
}
