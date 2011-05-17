using System;
using NServiceBus.Unicast.Distributor;
using System.Messaging;
using NServiceBus.Utils;

namespace NServiceBus.Distributor.MsmqWorkerAvailabilityManager
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
        public Address StorageQueue
        {
            get; set;
        }

	    /// <summary>
		/// Removes all entries from the worker availability queue
		/// with the specified address.
		/// </summary>
		/// <param name="address">
		/// The address of the worker to remove from the availability list.
		/// </param>
        public void ClearAvailabilityForWorker(Address address)
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
        public Address PopAvailableWorker()
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

            storageQueue = new MessageQueue(path);

            if (!storageQueue.Transactional)
                throw new Exception("Queue must be transactional.");
	    }

	    /// <summary>
		/// Signal that a worker is available to receive a dispatched message.
		/// </summary>
		/// <param name="address">
		/// The address of the worker that will accept the dispatched message.
		/// </param>
        public void WorkerAvailable(Address address)
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
