namespace NServiceBus.Distributor.MsmqWorkerAvailabilityManager
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Messaging;
    using System.Threading;
    using Settings;
    using Transports.Msmq;
    using Unicast.Distributor;

    /// <summary>
    /// An implementation of <see cref="IWorkerAvailabilityManager"/> for MSMQ to be used
    /// with the <see cref="DistributorSatellite"/> class.
    /// </summary>
    public class MsmqWorkerAvailabilityManager : IWorkerAvailabilityManager
    {
        MessageQueue storageQueue;
        readonly object lockObject = new object();

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
            lock (lockObject)
            {
                var messages = storageQueue.GetAllMessages();

                foreach (var m in messages.Where(m => MsmqUtilities.GetIndependentAddressForQueue(m.ResponseQueue) == address))
                {
                    storageQueue.ReceiveById(m.Id, MessageQueueTransactionType.Automatic);
                }
            }
        }

        /// <summary>
        /// Pops the next available worker from the available worker queue
        /// and returns its address.
        /// </summary>
        [DebuggerNonUserCode]
        public Address PopAvailableWorker()
        {
            if (!Monitor.TryEnter(lockObject))
            {
                return null;
            }

            try
            {
                var m = storageQueue.Receive(TimeSpan.Zero, MessageQueueTransactionType.Automatic);

                if (m == null)
                {
                    return null;
                }

                return MsmqUtilities.GetIndependentAddressForQueue(m.ResponseQueue);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                Monitor.Exit(lockObject);
            }
        }

        /// <summary>
        /// Initializes the object.
        /// </summary>
        public void Start()
        {
            var path = MsmqUtilities.GetFullPath(StorageQueueAddress);

            storageQueue = new MessageQueue(path);

            if ((!storageQueue.Transactional) && (SettingsHolder.Get<bool>("Transactions.Enabled")))
            {
                throw new Exception(string.Format("Queue [{0}] must be transactional.", path));
            }
        }

        public void Stop()
        {
            if (storageQueue != null)
            {
                storageQueue.Dispose();
            }
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
