namespace NServiceBus.Unicast.Transport.Transactional.DequeueStrategies
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Schedulers;
    using Logging;
    using Queuing.Msmq;
    using Utils;

    public class MsmqDequeueStrategy : IDequeueMessages
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (MsmqDequeueStrategy));
        private SemaphoreSlim semaphore;
        private TransactionSettings transactionSettings;
        private MTATaskScheduler scheduler;

        public MsmqMessageReceiver Receiver { get; set; }

        public void Init(Address address, TransactionSettings transactionSettings)
        {
            this.transactionSettings = transactionSettings;
            Receiver.Init(address, transactionSettings.IsTransactional);
            Receiver.MessageIsAvailable += OnMessageIsAvailable;
            Receiver.CriticalExceptionEncountered += OnCriticalExceptionEncountered;
        }

        public void Start(int maxDegreeOfParallelism)
        {
            scheduler = new MTATaskScheduler(maxDegreeOfParallelism);
            semaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
            Receiver.Start();
        }

        public void ChangeMaxDegreeOfParallelism(int value)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            Receiver.Stop();
            scheduler.Dispose();
            semaphore.Dispose();
        }

        public event EventHandler<TransportMessageAvailableEventArgs> MessageDequeued;

        private void OnMessageIsAvailable(object sender, MessageIsAvailableEventArgs args)
        {
            semaphore.Wait();

            Task.Factory.StartNew(() =>
                {
                    if (transactionSettings.IsTransactional)
                    {
                        new TransactionWrapper().RunInTransaction(() => FireMessageDequeueEvent(args),
                                                                  transactionSettings.IsolationLevel,
                                                                  transactionSettings.TransactionTimeout);
                    }
                    else
                    {
                        FireMessageDequeueEvent(args);
                    }
                }, CancellationToken.None, TaskCreationOptions.None, scheduler)
                .ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Logger.Error("Error in receiving messages.", task.Exception);
                        }
                    })
                .ContinueWith(task => semaphore.Release())
                .ContinueWith(task => { AggregateException ignore = task.Exception; });
        }

        private void FireMessageDequeueEvent(MessageIsAvailableEventArgs args)
        {
            TransportMessage message = null;

            try
            {
                message = args.Message;
            }
            catch (Exception ex)
            {
                Logger.Error("Error in receiving messages.", ex);
            }


            if (message != null)
            {
                MessageDequeued(this, new TransportMessageAvailableEventArgs(message));
            }
        }

        private void OnCriticalExceptionEncountered(object sender, CriticalExceptionEncounteredEventArgs args)
        {
            Logger.Fatal("Error in receiving messages.", args.Exception);

            Configure.Instance.OnCriticalError(String.Format("Error in receiving messages.\n{0}", args.Exception));
        }
    }
}