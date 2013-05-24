namespace NServiceBus.Unicast.Queuing.Azure
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using CircuitBreakers;
    using Transport;
    using Transports;

    /// <summary>
    /// A polling implementation of <see cref="IDequeueMessages"/>.
    /// </summary>
    public class PollingDequeueStrategy : IDequeueMessages
    {  
        /// <summary>
        /// See <see cref="IReceiveMessages"/>.
        /// </summary>
        public AzureMessageQueueReceiver MessageReceiver { get; set; }

        /// <summary>
        /// Initializes the <see cref="IDequeueMessages"/>.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="transactionSettings">The <see cref="TransactionSettings"/> to be used by <see cref="IDequeueMessages"/>.</param>
        /// <param name="tryProcessMessage">Called when a message has been dequeued and is ready for processing.</param>
        /// <param name="endProcessMessage">Needs to be called by <see cref="IDequeueMessages"/> after the message has been processed regardless if the outcome was successful or not.</param>
        public void Init(Address address, TransactionSettings transactionSettings, Func<TransportMessage, bool> tryProcessMessage, Action<TransportMessage, Exception> endProcessMessage)
        {
            this.tryProcessMessage = tryProcessMessage;
            this.endProcessMessage = endProcessMessage;

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
        }

        void StartThread()
        {
            var token = tokenSource.Token;

            Task.Factory
                .StartNew(Action, token, token, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .ContinueWith(t =>
                    {
                        t.Exception.Handle(ex =>
                            {
                                circuitBreaker.Execute(() => Configure.Instance.RaiseCriticalError(string.Format("Failed to receive message from '{0}'.", MessageReceiver), ex));
                                return true;
                            });

                        StartThread();
                    }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void Action(object obj)
        {
            var cancellationToken = (CancellationToken)obj;

            while (!cancellationToken.IsCancellationRequested)
            {
                Exception exception = null;
                TransportMessage message = null;

                try
                {
                    if (settings.IsTransactional)
                    {
                        using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
                        {
                            message = MessageReceiver.Receive();

                            if (message != null)
                            {
                                if (tryProcessMessage(message))
                                {
                                    scope.Complete();
                                }
                            }
                        }
                    }
                    else
                    {
                        message = MessageReceiver.Receive();

                        if (message != null)
                        {
                            tryProcessMessage(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    endProcessMessage(message, exception);
                }
            }
        }

        readonly CircuitBreaker circuitBreaker = new CircuitBreaker(100, TimeSpan.FromSeconds(30));
        Func<TransportMessage, bool> tryProcessMessage;
        CancellationTokenSource tokenSource;
        Address addressToPoll;
        TransactionSettings settings;
        TransactionOptions transactionOptions;
        Action<TransportMessage, Exception> endProcessMessage;
    }
}