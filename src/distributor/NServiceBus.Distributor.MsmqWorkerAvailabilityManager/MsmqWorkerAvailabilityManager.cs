using System;
using NServiceBus.Unicast.Distributor;
using System.Messaging;
using NServiceBus.Config;

namespace NServiceBus.Distributor.MsmqWorkerAvailabilityManager
{
    using Unicast.Queuing.Msmq;

    /// <summary>
    /// An implementation of <see cref="IWorkerAvailabilityManager"/> for MSMQ to be used
    /// with the <see cref="Distributor"/> class.
    /// </summary>
    public class MsmqWorkerAvailabilityManager : IWorkerAvailabilityManager
    {
        MessageQueue storageQueue;

        /// <summary>
        /// Sets the path to the queue that will be used for storing
        /// worker availability.
        /// </summary>
        /// <remarks>The queue provided must be transactional.</remarks>
        public Address StorageQueueAddress { get; set; }

        /// <summary>
        /// Removes all entries from the worker availability queue
        /// with the specified address.
        /// </summary>
        /// <param name="address">
        /// The address of the worker to remove from the availability list.
        /// </param>
        public void ClearAvailabilityForWorker(Address address)
        {
            var existing = storageQueue.GetAllMessages();

            foreach (var m in existing)
                if (MsmqUtilities.GetIndependentAddressForQueue(m.ResponseQueue) == address)
                    storageQueue.ReceiveById(m.Id, MessageQueueTransactionType.Automatic);
        }

        /// <summary>
        /// Pops the next available worker from the available worker queue
        /// and returns its address.
        /// </summary>
        public Address PopAvailableWorker()
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

        /// <summary>
        /// Initializes the object.
        /// </summary>
        public void Start()
        {
            var path = MsmqUtilities.GetFullPath(StorageQueueAddress);

            storageQueue = new MessageQueue(path);

            if ((!storageQueue.Transactional) && (!Endpoint.IsVolatile))
                throw new Exception(string.Format("Queue [{0}] must be transactional.", path));
        }

        /// <summary>
        /// Signal that a worker is available to receive a dispatched message.
        /// </summary>
        /// <param name="address">
        /// The address of the worker that will accept the dispatched message.
        /// </param>
        /// <param name="capacity">The number of messages that this worker is ready to process</param>
        public void WorkerAvailable(Address address, int capacity)
        {
            for (var i = 0; i < capacity; i++)
                storageQueue.Send(new Message
                              {
                                  ResponseQueue = new MessageQueue(MsmqUtilities.GetFullPath(address))
                              }, MessageQueueTransactionType.Automatic);
        }
    }
}
