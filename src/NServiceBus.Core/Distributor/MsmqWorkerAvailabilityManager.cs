namespace NServiceBus.Transports.Msmq.WorkerAvailabilityManager
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Messaging;
    using System.Threading;
    using Distributor;
    using Settings;

    /// <summary>
    /// An implementation of <see cref="IWorkerAvailabilityManager"/> for MSMQ to be used
    /// with the <see cref="DistributorSatellite"/> class.
    /// </summary>
    [ObsoleteEx(Message = "Not a public API.", TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]
    public class MsmqWorkerAvailabilityManager : IWorkerAvailabilityManager
    {
        /// <summary>
        /// Msmq unit of work to be used in non DTC mode <see cref="MsmqUnitOfWork"/>.
        /// </summary>
        public MsmqUnitOfWork UnitOfWork { get; set; }

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
                    if (UnitOfWork.HasActiveTransaction())
                    {
                        storageQueue.ReceiveById(m.Id, UnitOfWork.Transaction);
                    }
                    else
                    {
                        storageQueue.ReceiveById(m.Id, MessageQueueTransactionType.Automatic);
                    }

                    
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
                Message availableWorker;

                if (UnitOfWork.HasActiveTransaction())
                {
                    availableWorker = storageQueue.Receive(MaxTimeToWaitForAvailableWorker, UnitOfWork.Transaction);
                }
                else
                {
                    availableWorker = storageQueue.Receive(MaxTimeToWaitForAvailableWorker, MessageQueueTransactionType.Automatic);                    
                }

                if (availableWorker == null)
                {
                    return null;
                }

                return MsmqUtilities.GetIndependentAddressForQueue(availableWorker.ResponseQueue);
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
            var returnAddress = new MessageQueue(MsmqUtilities.GetFullPath(address));

            for (var i = 0; i < capacity; i++)
            {
                if (UnitOfWork.HasActiveTransaction())
                {
                    storageQueue.Send(new Message{ResponseQueue = returnAddress}, UnitOfWork.Transaction); 
                }
                else
                {
                    storageQueue.Send(new Message{ResponseQueue = returnAddress}, MessageQueueTransactionType.Automatic); 
                }
            }
        }

        static TimeSpan MaxTimeToWaitForAvailableWorker = TimeSpan.FromSeconds(10);

        MessageQueue storageQueue;
        readonly object lockObject = new object();
    }
}
