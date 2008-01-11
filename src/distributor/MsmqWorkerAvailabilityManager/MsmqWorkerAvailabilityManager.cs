using System;
using NServiceBus.Unicast.Distributor;
using System.Messaging;

namespace MsmqWorkerAvailabilityManager
{
	/// <summary>
	/// An implementation of <see cref="IWorkerAvailabilityManager"/> for MSMQ to be used
	/// with the <see cref="Distributor"/> class.
	/// </summary>
    public class MsmqWorkerAvailabilityManager : IWorkerAvailabilityManager
    {
        #region config info

        private MessageQueue storageQueue;

		/// <summary>
		/// Sets the path to the queue that will be used for storing
		/// worker availability.
		/// </summary>
		/// <remarks>The queue provided must be transactional.</remarks>
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

		/// <summary>
		/// Removes all entries from the worker availability queue
		/// with the specified address.
		/// </summary>
		/// <param name="address">
		/// The address of the worker to remove from the availability list.
		/// </param>
        public void ClearAvailabilityForWorker(string address)
        {
            Message[] existing = this.storageQueue.GetAllMessages();

            foreach (Message m in existing)
                if (m.ResponseQueue.Path == address)
                    this.storageQueue.ReceiveById(m.Id, MessageQueueTransactionType.Automatic);
        }

		/// <summary>
		/// Pops the next available worker from the available worker queue
		/// and returns its address.
		/// </summary>
		/// <returns>The address of the next available worker.</returns>
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

                    this.storageQueue.ReceiveById(existing[0].Id, MessageQueueTransactionType.Automatic);

                    return path;
                }
            }
        }

		/// <summary>
		/// Signal that a worker is available to receive a dispatched message.
		/// </summary>
		/// <param name="address">
		/// The address of the worker that will accept the dispatched message.
		/// </param>
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
