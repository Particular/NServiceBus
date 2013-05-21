namespace NServiceBus.Transports.RabbitMQ
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Schedulers;
    using CircuitBreakers;
    using Logging;
    using Unicast.Transport;
    using global::RabbitMQ.Client;
    using global::RabbitMQ.Client.Events;

    /// <summary>
    ///     Default implementation of <see cref="IDequeueMessages" /> for RabbitMQ.
    /// </summary>
    public class RabbitMqDequeueStrategy : IDequeueMessages
    {
        /// <summary>
        /// The connection to the RabbitMQ broker
        /// </summary>
        public IManageRabbitMqConnections ConnectionManager { get; set; }


        /// <summary>
        /// Determines if the queue should be purged when the transport starts
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        /// <summary>
        /// The number of messages to allow the RabbitMq client to pre-fetch from the broker
        /// </summary>
        public ushort PrefetchCount { get; set; }

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
            workQueue = address.Queue;
            autoAck = !transactionSettings.IsTransactional;
        }

        /// <summary>
        /// Starts the dequeuing of message using the specified <paramref name="maximumConcurrencyLevel"/>.
        /// </summary>
        /// <param name="maximumConcurrencyLevel">Indicates the maximum concurrency level this <see cref="IDequeueMessages"/> is able to support.</param>
        public void Start(int maximumConcurrencyLevel)
        {
            if (PurgeOnStartup)
                Purge();

            scheduler = new MTATaskScheduler(maximumConcurrencyLevel,
                                             String.Format("NServiceBus Dequeuer Worker Thread for [{0}]", workQueue));

            for (int i = 0; i < maximumConcurrencyLevel; i++)
            {
                StartConsumer();
            }
        }

        /// <summary>
        /// Stops the dequeuing of messages.
        /// </summary>
        public void Stop()
        {
            tokenSource.Cancel();

            if (scheduler != null)
            {
                scheduler.Dispose();
            }
        }

        void StartConsumer()
        {
            var token = tokenSource.Token;

            Task.Factory
                .StartNew(Action, token, token, TaskCreationOptions.None, scheduler)
                .ContinueWith(t =>
                    {
                        t.Exception.Handle(ex =>
                            {
                                circuitBreaker.Failure(ex);
                                return true;
                            });

                        StartConsumer();
                    }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void Action(object obj)
        {
            var cancellationToken = (CancellationToken)obj;
            var connection = ConnectionManager.GetConnection(ConnectionPurpose.Consume);

            using (var channel = connection.CreateModel())
            {
                channel.BasicQos(0, PrefetchCount, false);

                var consumer = new QueueingBasicConsumer(channel);

                channel.BasicConsume(workQueue, autoAck, consumer);

                circuitBreaker.Success();

                while (!cancellationToken.IsCancellationRequested)
                {
                    Exception exception = null;
                    BasicDeliverEventArgs message = null;

                    message = DequeueMessage(consumer);

                    if (message == null)
                    {
                        continue;
                    }

                    TransportMessage transportMessage = null;
                  
                    try
                    {
                        var messageProcessedOk = false;

                        try
                        {
                            transportMessage = RabbitMqTransportMessageExtensions.ToTransportMessage(message);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Poison message detected, deliveryTag: " + message.DeliveryTag, ex);

                            //just ack the poison message to avoid getting stuck
                            messageProcessedOk = true;
                        }

                        if (transportMessage != null)
                        {
                            messageProcessedOk = tryProcessMessage(transportMessage);
                        }

                        if (!autoAck)
                        {
                            if (messageProcessedOk)
                                channel.BasicAck(message.DeliveryTag, false);
                            else
                                channel.BasicReject(message.DeliveryTag, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;

                        if (!autoAck)
                            channel.BasicReject(message.DeliveryTag, true);
                    }
                    finally
                    {
                        endProcessMessage(transportMessage, exception);
                    }
                }
            }
        }

        static BasicDeliverEventArgs DequeueMessage(QueueingBasicConsumer consumer)
        {
            object rawMessage;

            if (!consumer.Queue.Dequeue(1000, out rawMessage))
            {
                return null;
            }

            return (BasicDeliverEventArgs)rawMessage;
        }

        void Purge()
        {
            using (var channel = ConnectionManager.GetConnection(ConnectionPurpose.Administration).CreateModel())
            {
                channel.QueuePurge(workQueue);
            }
        }

        readonly ICircuitBreaker circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("RabbitMqConnectivity",
                    TimeSpan.FromMinutes(2),
                    ex => Configure.Instance.RaiseCriticalError("Repeated failures when communicating with the RabbitMq broker", ex),
                    TimeSpan.FromSeconds(5));
        
        Func<TransportMessage, bool> tryProcessMessage;
        bool autoAck;
        MTATaskScheduler scheduler;
        readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
        string workQueue;
        Action<TransportMessage, Exception> endProcessMessage;

        static ILog Logger = LogManager.GetLogger(typeof(RabbitMqDequeueStrategy));
    }
}