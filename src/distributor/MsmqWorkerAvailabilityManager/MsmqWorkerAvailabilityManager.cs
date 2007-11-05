using System;
using System.Collections.Generic;
using System.Text;
using NServiceBus.Unicast.Distributor;
using System.Messaging;

namespace MsmqWorkerAvailabilityManager
{
    public class MsmqWorkerAvailabilityManager : IWorkerAvailabilityManager
    {
        #region config info

        private MessageQueue storageQueue;
        public string StorageQueue
        {
            set 
            {
                MessageQueue q = new MessageQueue(value);

                if (!q.Transactional)
                    throw new Exception("Queue must be transactional.");

                this.storageQueue = q;
            }
        }

        #endregion

        #region IWorkerAvailabilityManager Members

        public void ClearAvailabilityForWorker(string address)
        {
            Message[] existing = this.storageQueue.GetAllMessages();

            foreach (Message m in existing)
                if (m.ResponseQueue.Path == address)
                    this.storageQueue.ReceiveById(m.Id, MessageQueueTransactionType.Automatic);
        }

        public string PopAvailableWorker()
        {
            lock (locker)
            {
                Message[] existing = this.storageQueue.GetAllMessages();

                if (existing.Length == 0)
                    return null;
                else
                {
                    string path = existing[0].ResponseQueue.Path;

                    Message m = this.storageQueue.ReceiveById(existing[0].Id, MessageQueueTransactionType.Automatic);

                    return path;
                }
            }
        }

        public void WorkerAvailable(string address)
        {
            lock (locker)
            {
                Message msg = new Message();
                msg.ResponseQueue = new MessageQueue(address);

                this.storageQueue.Send(msg, MessageQueueTransactionType.Automatic);
            }
        }

        #endregion

        #region members

        private object locker = new object();

        #endregion
    }
}
