namespace NServiceBus.Unicast.Transport.Transactional.DequeueStrategies
{
    using System;
    using System.Diagnostics;
    using Logging;
    using Queuing;
    using ThreadingStrategies;
    using Utils;

    public class PollingDequeueStrategy:IDequeueMessages
    {
        public IReceiveMessages MessageReceiver { get; set; }

      
        public void Init(Address address, TransactionSettings transactionSettings)
        {
            addressToPoll = address;
            settings = transactionSettings;
        }

        public void Start(int maxDegreeOfParallelism)
        {
            MessageReceiver.Init(addressToPoll, settings.IsTransactional);

            threadingStrategy = new StaticThreadingStrategy();

            threadingStrategy.Start(maxDegreeOfParallelism, Poll);
        }

        public void ChangeMaxDegreeOfParallelism(int value)
        {
            threadingStrategy.ChangeMaxDegreeOfParallelism(value);
        }

        void Poll()
        {
            try
            {
                if (settings.IsTransactional)
                    new TransactionWrapper().RunInTransaction(TryReceive, settings.IsolationLevel, settings.TransactionTimeout); 
                else
                    TryReceive();
            }
            catch (Exception ex)
            {
                Logger.Debug("Failed to process transport message", ex);
            }

        }

        void TryReceive()
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

                Configure.Instance.OnCriticalError();

                return null;
            }
            catch (Exception e)
            {
                Logger.Error("Error in receiving messages.", e);
                return null;
            }
        }


        public void Stop()
        {
            threadingStrategy.Stop();
        }

        public event EventHandler<TransportMessageAvailableEventArgs> MessageDequeued;

        IThreadingStrategy threadingStrategy;
        Address addressToPoll;
        TransactionSettings settings;

        static readonly ILog Logger = LogManager.GetLogger(typeof(PollingDequeueStrategy));

    }
}