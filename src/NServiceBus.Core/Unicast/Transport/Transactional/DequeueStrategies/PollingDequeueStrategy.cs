namespace NServiceBus.Unicast.Transport.Transactional.DequeueStrategies
{
    using System;
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
        /// <summary>
        /// See <see cref="IReceiveMessages"/>.
        /// </summary>
        public IReceiveMessages MessageReceiver { get; set; }

        /// <summary>
        /// Initialises the <see cref="IDequeueMessages"/>.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="transactionSettings">The <see cref="TransactionSettings"/> to be used by <see cref="IDequeueMessages"/>.</param>
        /// <param name="tryProcessMessage"></param>
        public void Init(Address address, TransactionSettings transactionSettings,Func<TransportMessage, bool> tryProcessMessage)
        {
            this.tryProcessMessage = tryProcessMessage;

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

            for (int i = 0; i < maximumConcurrencyLevel; i++)
            {
                StartThread();
            }
        }

        /// <summary>
        /// Stops the dequeuing of messages.
        /// </summary>
        public void Stop()
        {
            tokenSource.Cancel();
            scheduler.Dispose();
        }

        void StartThread()
        {
            var token = tokenSource.Token;

            Task.Factory.StartNew(obj =>
                {
                    var cancellationToken = (CancellationToken)obj;

                    while (!cancellationToken.IsCancellationRequested)
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
                }, token, token, TaskCreationOptions.None, scheduler)
                .ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        Configure.Instance.OnCriticalError(string.Format("Failed to receive message from '{0}'.", MessageReceiver), t.Exception);
                        StartThread();
                    }
                });
        }

        bool TryReceive()
        {
            var m = MessageReceiver.Receive();
            return m != null && tryProcessMessage(m);
        }

        Func<TransportMessage, bool> tryProcessMessage;
        CancellationTokenSource tokenSource;
        Address addressToPoll;
        MTATaskScheduler scheduler;
        TransactionSettings settings;
        TransactionOptions transactionOptions;
    }
}