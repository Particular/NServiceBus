namespace NServiceBus.Distributor.MSMQ
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Messaging;
    using System.Threading;
    using Logging;
    using Settings;
    using Transports.Msmq;

    /// <summary>
    ///     An implementation of <see cref="IWorkerAvailabilityManager" /> for MSMQ to be used
    ///     with the <see cref="DistributorSatellite" /> class.
    /// </summary>
    internal class MsmqWorkerAvailabilityManager : IWorkerAvailabilityManager, IDisposable
    {
        public MsmqWorkerAvailabilityManager()
        {
            var storageQueueAddress = Address.Local.SubScope("distributor.storage");
            var path = MsmqUtilities.GetFullPath(storageQueueAddress);
            var messageReadPropertyFilter = new MessagePropertyFilter
            {
                Id = true,
                Label = true,
                ResponseQueue = true,
            };

            storageQueue = new MessageQueue(path, false, true, QueueAccessMode.SendAndReceive)
            {
                MessageReadPropertyFilter = messageReadPropertyFilter
            };

            if ((!storageQueue.Transactional) && (SettingsHolder.Get<bool>("Transactions.Enabled")))
            {
                throw new Exception(string.Format("Queue [{0}] must be transactional.", path));
            }
        }

        /// <summary>
        ///     Msmq unit of work to be used in non DTC mode <see cref="MsmqUnitOfWork" />.
        /// </summary>
        public MsmqUnitOfWork UnitOfWork { get; set; }

        public void Dispose()
        {
            //Injected
        }

        /// <summary>
        ///     Pops the next available worker from the available worker queue
        ///     and returns its address.
        /// </summary>
        [DebuggerNonUserCode]
        public Worker NextAvailableWorker()
        {
            try
            {
                Message availableWorker;

                if (!storageLock.TryEnterReadLock(MaxTimeToWaitForAvailableWorker))
                {
                    return null;
                }

                try
                {
                    if (UnitOfWork.HasActiveTransaction())
                    {
                        availableWorker = storageQueue.Receive(MaxTimeToWaitForAvailableWorker, UnitOfWork.Transaction);
                    }
                    else
                    {
                        availableWorker = storageQueue.Receive(MaxTimeToWaitForAvailableWorker, MessageQueueTransactionType.Automatic);
                    }
                }
                finally
                {
                    storageLock.ExitReadLock();
                }

                if (availableWorker == null)
                {
                    return null;
                }

                var address = MsmqUtilities.GetIndependentAddressForQueue(availableWorker.ResponseQueue);
                string registeredWorkerSessionId;
                var sessionId = availableWorker.Label;

                if (String.IsNullOrEmpty(sessionId)) //Old worker
                {
                    Logger.InfoFormat("Using an old version Worker at '{0}'.", address);
                    return new Worker(address, sessionId);
                }

                if (!registeredWorkerAddresses.TryGetValue(address, out registeredWorkerSessionId))
                {
                    // Distributor could have been restarted, hence the reason we do not have the worker registered.
                    registeredWorkerAddresses[address] = sessionId;

                    Logger.InfoFormat("Worker at '{0}' has been re-registered with distributor.", address);
                }

                return new Worker(address, sessionId);
            }
            catch (MessageQueueException e)
            {
                Logger.InfoFormat("NextAvailableWorker Exception", e);
                return null;
            }
        }

        public void WorkerAvailable(Worker worker)
        {
            string sessionId;

            if (!registeredWorkerAddresses.TryGetValue(worker.Address, out sessionId))
            {
                // The worker send us a message before the "WorkerStarting" message
                Logger.InfoFormat("Dropping ready message from Worker at '{0}', because this worker worker sent us a message before the 'WorkerStarting' message.", worker.Address);

                return;
            }

            if (sessionId.Equals("disconnected"))
            {
                // Drop ready message as this worker has been disconnected 
                Logger.InfoFormat("Dropping ready message from Worker at '{0}', because this worker has been disconnected.", worker.Address);

                return;
            }

            Logger.InfoFormat("Worker at '{0}' is available to take on more work.", worker.Address);

            AddWorkerToStorageQueue(worker);
        }

        public void UnregisterWorker(Address address)
        {
            registeredWorkerAddresses[address] = "disconnected";
        }

        public void RegisterNewWorker(Worker worker, int capacity)
        {
            // Need to handle backwards compatibility
            if (worker.SessionId == String.Empty)
            {
                ClearAvailabilityForWorker(worker.Address);
            }

            AddWorkerToStorageQueue(worker, capacity);

            registeredWorkerAddresses[worker.Address] = worker.SessionId;

            Logger.InfoFormat("Worker '{0}' has been registered with {1} capacity.", worker.Address, capacity);
        }

        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "6.0")]
        void ClearAvailabilityForWorker(Address address)
        {
            storageLock.EnterWriteLock();

            try
            {
                var messages = storageQueue.GetAllMessages();

                Logger.InfoFormat("Clearing availability for worker {0} with {1} messages", address, messages.Count());

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
            finally
            {
                storageLock.ExitWriteLock();
            }
        }

        void AddWorkerToStorageQueue(Worker worker, int capacity = 1)
        {
            using (var returnAddress = new MessageQueue(MsmqUtilities.GetFullPath(worker.Address), false, true, QueueAccessMode.Send))
            {
                var message = new Message
                {
                    Label = worker.SessionId,
                    ResponseQueue = returnAddress
                };

                for (var i = 0; i < capacity; i++)
                {
                    if (UnitOfWork.HasActiveTransaction())
                    {
                        storageQueue.Send(message, UnitOfWork.Transaction);
                    }
                    else
                    {
                        storageQueue.Send(message, MessageQueueTransactionType.Automatic);
                    }
                }
            }
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(MsmqWorkerAvailabilityManager));

        static TimeSpan MaxTimeToWaitForAvailableWorker = TimeSpan.FromSeconds(10);
        ReaderWriterLockSlim storageLock = new ReaderWriterLockSlim();
        Dictionary<Address, string> registeredWorkerAddresses = new Dictionary<Address, string>();
        MessageQueue storageQueue;
    }
}