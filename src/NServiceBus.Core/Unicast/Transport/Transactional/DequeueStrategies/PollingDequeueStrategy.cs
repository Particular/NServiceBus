namespace NServiceBus.Unicast.Transport.Transactional.DequeueStrategies
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using Queuing;
    using Utils;

    public class PollingDequeueStrategy : IDequeueMessages
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (PollingDequeueStrategy));
        private readonly IList<WorkerThread> workerThreads = new List<WorkerThread>();
        private Address addressToPoll;
        private TransactionSettings settings;

        public IReceiveMessages MessageReceiver { get; set; }

        public void Init(Address address, TransactionSettings transactionSettings)
        {
            addressToPoll = address;
            settings = transactionSettings;
        }

        public void Start(int maximumConcurrencyLevel)
        {
            MessageReceiver.Init(addressToPoll, settings.IsTransactional);

            StartThreads(maximumConcurrencyLevel);
        }

        public void Stop()
        {
            StopThreads();
        }

        public event EventHandler<TransportMessageAvailableEventArgs> MessageDequeued;

        private void StartThreads(int maximumConcurrencyLevel)
        {
            for (int i = 0; i < maximumConcurrencyLevel; i++)
                AddWorkerThread().Start();
        }

        private void StopThreads()
        {
            for (int i = 0; i < workerThreads.Count; i++)
                workerThreads[i].Stop();
        }

        private WorkerThread AddWorkerThread()
        {
            var result = new WorkerThread(Poll);

            workerThreads.Add(result);

            result.Stopped += delegate(object sender, EventArgs e)
                {
                    var wt = sender as WorkerThread;
                    lock (workerThreads)
                        workerThreads.Remove(wt);
                };

            return result;
        }

        private void Poll()
        {
            try
            {
                if (settings.IsTransactional)
                    new TransactionWrapper().RunInTransaction(TryReceive, settings.IsolationLevel,
                                                              settings.TransactionTimeout);
                else
                    TryReceive();
            }
            catch (Exception ex)
            {
                Logger.Debug("Failed to process transport message", ex);
            }
        }

        private void TryReceive()
        {
            TransportMessage m = Receive();
            if (m == null)
                return;

            MessageDequeued(this, new TransportMessageAvailableEventArgs(m));
        }

        private TransportMessage Receive()
        {
            try
            {
                return MessageReceiver.Receive();
            }
            catch (InvalidOperationException e)
            {
                Logger.Fatal("Error in receiving messages.", e);

                Configure.Instance.OnCriticalError(String.Format("Error in receiving messages.\n{0}", e));
            }
            catch (Exception e)
            {
                Logger.Error("Error in receiving messages.", e);
            }

            return null;
        }
    }
}