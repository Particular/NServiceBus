using System;
using System.Messaging;
using System.Transactions;
using NServiceBus.Unicast.Transport.Msmq;
using NServiceBus.Utils;

namespace NServiceBus.Tools.Management.Errors.ReturnToSourceQueue
{
    public class Class1
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
            try
            {
                ReturnMessage(messageId);
            }
            catch (MessageQueueException ex)
            {
                if (ex.MessageQueueErrorCode != MessageQueueErrorCode.IOTimeout)
                {
                    Console.WriteLine("Could not return message to source queue.\nReason: " + ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }

                Console.WriteLine("Message ID not found in time. Going to look in message labels for original ID.");

                foreach (var m in queue.GetAllMessages())
                {
                    var id = MsmqTransport.GetRealMessageId(m);
                    if (id == messageId)
                    {
                        try
                        {
                            ReturnMessage(m.Id);
                            break;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Could not return message to source queue.\nReason: " + e.Message);
                            Console.WriteLine(e.StackTrace);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not return message to source queue.\nReason: " + e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private void ReturnMessage(string messageId)
        {
            using (var scope = new TransactionScope())
            {
                var m = queue.ReceiveById(messageId, TimeSpan.FromSeconds(5), MessageQueueTransactionType.Automatic);

                var failedQueue = MsmqTransport.GetFailedQueue(m);

                m.Label = MsmqTransport.GetLabelWithoutFailedQueue(m);

                using (var q = new MessageQueue(failedQueue))
                {
                    Console.WriteLine("Returning message with id " + messageId + " to queue " + failedQueue);
                    q.Send(m, MessageQueueTransactionType.Automatic);
                }

                scope.Complete();
            }
        }
    }
}
