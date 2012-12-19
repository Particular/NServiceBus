namespace NServiceBus.Unicast.Transport.Transactional.DequeueStrategies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Schedulers;
    using System.Transactions;
    using Queuing;

    /// <summary>
    /// A polling implementation of <see cref="IDequeueMessages"/>.
    /// </summary>
    public class PollingDequeueStrategy : IDequeueMessages
    {
        private CancellationTokenSource tokenSource;
        private readonly IList<Task> runningTasks = new List<Task>();
        private Address addressToPoll;
        private MTATaskScheduler scheduler;
        private TransactionSettings settings;
        private TransactionOptions transactionOptions;
       
        /// <summary>
        /// See <see cref="IReceiveMessages"/>.
        /// </summary>
        public IReceiveMessages MessageReceiver { get; set; }

        /// <summary>
        /// Initialises the <see cref="IDequeueMessages"/>.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="transactionSettings">The <see cref="TransactionSettings"/> to be used by <see cref="IDequeueMessages"/>.</param>
        public void Init(Address address, TransactionSettings transactionSettings)
        {
            addressToPoll = address;
            settings = transactionSettings;
            transactionOptions = new TransactionOptions { IsolationLevel = transactionSettings.IsolationLevel, Timeout = transactionSettings.TransactionTimeout };

            MessageReceiver.Init(addressToPoll, settings.IsTransactional);
        }

        /// <summary>
        /// Starts the dequeuing of message using the specified <paramref name="maximumConcurrencyLevel"/>.
        /// </summary>
        /// <param name="maximumConcurrencyLevel">Indicates the maximum concurrency level this <see cref="IDequeueMessages"/> is able to support.</param>
        public void Start(int maximumConcurrencyLevel)
        {
            tokenSource = new CancellationTokenSource();

            scheduler = new MTATaskScheduler(maximumConcurrencyLevel,
                                             String.Format("NServiceBus Dequeuer Worker Thread for [{0}]", addressToPoll));

            StartThreads(maximumConcurrencyLevel);
        }

        /// <summary>
        /// Stops the dequeuing of messages.
        /// </summary>
        public void Stop()
        {
            tokenSource.Cancel();
            scheduler.Dispose();
            runningTasks.Clear();
        }

        /// <summary>
        /// Called when a message has been dequeued and is ready for processing.
        /// </summary>
        public Func<TransportMessage, bool> TryProcessMessage { get; set; }

        void StartThreads(int maximumConcurrencyLevel)
        {
            for (int i = 0; i < maximumConcurrencyLevel; i++)
            {
                var token = tokenSource.Token;

                runningTasks.Add(Task.Factory.StartNew(obj =>
                    {
                        var cancellationToken = (CancellationToken)obj;

                        while (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                if (settings.IsTransactional)
                                {
                                    using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
                                    {
                                        if (TryReceive())
                                            scope.Complete();
                                    }
                                }
                                else
                                {
                                    TryReceive();
                                }
                            }
                            catch (Exception ex)
                            {
                                Configure.Instance.OnCriticalError(string.Format("Failed to receive message from '{0}'.", MessageReceiver), ex);
                            }
                        }
                    }, token, token, TaskCreationOptions.None, scheduler));
            }
        }

        bool TryReceive()
        {
            var m = MessageReceiver.Receive();
            return m != null && TryProcessMessage(m);
        }
    }
}