using System;
using NServiceBus.Unicast.Distributor;
using System.Messaging;
using NServiceBus.Utils;

namespace MsmqWorkerAvailabilityManager
{
	/// <summary>
	/// An implementation of <see cref="IWorkerAvailabilityManager"/> for MSMQ to be used
	/// with the <see cref="Distributor"/> class.
	/// </summary>
    public class MsmqWorkerAvailabilityManager : IWorkerAvailabilityManager
    {
	    private MessageQueue storageQueue;

		/// <summary>
		/// Sets the path to the queue that will be used for storing
		/// worker availability.
		/// </summary>
		/// <remarks>The queue provided must be transactional.</remarks>
        public string StorageQueue
        {
            get { return s; }
            set 
            {
                s = value;

                MsmqUtilities.CreateQueueIfNecessary(value);

                var path = MsmqUtilities.GetFullPath(value);

                var q = new MessageQueue(path);

                if (!q.Transactional)
                    throw new Exception("Queue must be transactional.");

                storageQueue = q;
            }
        }
        private string s;

	    /// <summary>
		/// Removes all entries from the worker availability queue
		/// with the specified address.
		/// </summary>
		/// <param name="address">
		/// The address of the worker to remove from the availability list.
		/// </param>
        public void ClearAvailabilityForWorker(string address)
        {
            lock (locker)
            {
                var existing = storageQueue.GetAllMessages();

                foreach (var m in existing)
                    if (MsmqUtilities.GetIndependentAddressForQueue(m.ResponseQueue) == address)
                        storageQueue.ReceiveById(m.Id, MessageQueueTransactionType.Automatic);
            }
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
                try
                {
                    var m = storageQueue.Receive(TimeSpan.Zero, MessageQueueTransactionType.Automatic);

                    if (m == null)
                        return null;

                    return MsmqUtilities.GetIndependentAddressForQueue(m.ResponseQueue);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Initializes the object.
        /// </summary>
	    public void Start()
	    {
            var path = MsmqUtilities.GetFullPath(StorageQueue);

            MsmqUtilities.CreateQueueIfNecessary(StorageQueue);

            var q = new MessageQueue(path);

            if (!q.Transactional)
                throw new Exception("Queue must be transactional.");
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
                var msg = new Message
                              {
                                  ResponseQueue = new MessageQueue(MsmqUtilities.GetFullPath(address))
                              };

                storageQueue.Send(msg, MessageQueueTransactionType.Automatic);
            }
        }

	    private readonly object locker = new object();
    }
}
