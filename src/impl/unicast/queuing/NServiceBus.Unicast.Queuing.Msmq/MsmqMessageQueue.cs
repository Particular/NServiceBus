using System;
using System.Messaging;
using System.Transactions;
using NServiceBus.Utils;

namespace NServiceBus.Unicast.Queuing.Msmq
{
    public class MsmqMessageQueue : IMessageQueue
    {
        public void Send(QueuedMessage message, string destination, bool transactional)
        {
            var address = MsmqUtilities.GetFullPath(destination);

            using (var q = new MessageQueue(address, QueueAccessMode.Send))
            {
                var toSend = new Message();

                if (message.BodyStream != null)
                    toSend.BodyStream = message.BodyStream;

                if (message.CorrelationId != null)
                    toSend.CorrelationId = message.CorrelationId;

                toSend.Recoverable = message.Recoverable;

                if (!string.IsNullOrEmpty(message.ResponseQueue))
                    toSend.ResponseQueue = new MessageQueue(MsmqUtilities.GetFullPath(message.ResponseQueue));

                toSend.Label = message.Label;

                if (message.TimeToBeReceived < MessageQueue.InfiniteTimeout)
                    toSend.TimeToBeReceived = message.TimeToBeReceived;

                if (message.Extension != null)
                    toSend.Extension = message.Extension;

                toSend.AppSpecific = message.AppSpecific;

                try
                {
                    q.Send(toSend, GetTransactionTypeForSend(transactional));
                }
                catch (MessageQueueException ex)
                {
                    if (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueNotFound)
                        throw new QueueNotFoundException {Queue = destination };

                    throw;
                }

                message.Id = toSend.Id;
            }
        }

        public void Init(string queue, bool purge, int secondsToWaitForMessage)
        {
            secondsToWait = secondsToWaitForMessage;

            var machine = MsmqUtilities.GetMachineNameFromLogicalName(queue);

            if (machine.ToLower() != Environment.MachineName.ToLower())
                throw new InvalidOperationException("Input queue must be on the same machine as this process.");

            myQueue = new MessageQueue(MsmqUtilities.GetFullPath(queue));

            bool transactional;
            try
            {
                transactional = myQueue.Transactional;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("There is a problem with the input queue given: {0}. See the enclosed exception for details.", queue), ex);
            }

            if (!transactional)
                throw new ArgumentException("Queue must be transactional (" + queue + ").");

            var mpf = new MessagePropertyFilter();
            mpf.SetAll();

            myQueue.MessageReadPropertyFilter = mpf;

            if (purge)
                myQueue.Purge();
        }

        public bool HasMessage()
        {
            try
            {
                var m = myQueue.Peek(TimeSpan.FromSeconds(secondsToWait));
                return m != null;
            }
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return false;

                throw;
            }
        }

        public QueuedMessage Receive(bool transactional)
        {
            try
            {
                var m = myQueue.Receive(TimeSpan.FromSeconds(secondsToWait), GetTransactionTypeForReceive(transactional));
                if (m == null)
                    return null;

                return new QueuedMessage
                {
                    Id = m.Id,
                    BodyStream = m.BodyStream,
                    CorrelationId =
                        (m.CorrelationId == "00000000-0000-0000-0000-000000000000\\0"
                             ? null
                             : m.CorrelationId),
                    Extension = m.Extension,
                    Label = m.Label,
                    Recoverable = m.Recoverable,
                    TimeToBeReceived = m.TimeToBeReceived,
                    TimeSent = m.SentTime,
                    ResponseQueue = MsmqUtilities.GetIndependentAddressForQueue(m.ResponseQueue),
                    AppSpecific = m.AppSpecific,
                    LookupId = m.LookupId
                };
            }
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return null;

                throw;
            }
        }

        public void CreateQueue(string queue)
        {
            MsmqUtilities.CreateQueueIfNecessary(queue);
        }

        private static MessageQueueTransactionType GetTransactionTypeForReceive(bool transactional)
        {
            return transactional ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.None;
        }

        private static MessageQueueTransactionType GetTransactionTypeForSend(bool transactional)
        {
            if (transactional)
                return Transaction.Current != null ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.Single;

            return MessageQueueTransactionType.Single;
        }

        private MessageQueue myQueue;
        private int secondsToWait;
    }
}
