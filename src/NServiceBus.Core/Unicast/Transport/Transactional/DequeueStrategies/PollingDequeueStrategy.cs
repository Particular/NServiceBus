namespace NServiceBus.Unicast.Transport.Transactional.DequeueStrategies
{
    using System;
    using System.Diagnostics;
    using Logging;
    using Queuing;
    using ThreadingStrategies;
    using Utils;

    public class PollingDequeueStrategy : IDequeueMessages
    {
        public IReceiveMessages MessageReceiver { get; set; }
        public IThreadingStrategy ThreadingStrategy { get; set; }

        public void Init(Address address, TransactionSettings transactionSettings)
        {
            addressToPoll = address;
            settings = transactionSettings;
        }

        public void Start(int maxDegreeOfParallelism)
        {
            MessageReceiver.Init(addressToPoll, settings.IsTransactional);

            ThreadingStrategy.Start(maxDegreeOfParallelism, Poll);
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
            var m = Receive();
            if (m == null)
                return;

            MessageDequeued(this, new TransportMessageAvailableEventArgs(m));
        }

        [DebuggerNonUserCode] // so that exceptions don't interfere with debugging.
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

        public void Stop()
        {
            ThreadingStrategy.Stop();
        }

        public event EventHandler<TransportMessageAvailableEventArgs> MessageDequeued;

        private Address addressToPoll;
        private TransactionSettings settings;

        private static readonly ILog Logger = LogManager.GetLogger(typeof (PollingDequeueStrategy));

    }
}