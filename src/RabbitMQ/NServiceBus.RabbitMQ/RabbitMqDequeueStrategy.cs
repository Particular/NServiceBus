namespace NServiceBus.RabbitMQ
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Schedulers;
    using Unicast.Transport.Transactional;
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
        public IConnection Connection { get; set; }
        /// <summary>
        /// Determines if the queue should be purged when the transport starts
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        /// <summary>
        /// Initialises the <see cref="IDequeueMessages"/>.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="transactionSettings">The <see cref="TransactionSettings"/> to be used by <see cref="IDequeueMessages"/>.</param>
        /// <param name="tryProcessMessage"></param>
        public void Init(Address address, TransactionSettings transactionSettings, Func<TransportMessage, bool> tryProcessMessage)
        {
            TryProcessMessage = tryProcessMessage;
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
                scheduler.Dispose();

            runningConsumers.Clear();
        }

        
        void StartConsumer()
        {
            var token = tokenSource.Token;

            var task = new Task(obj =>
                {
                    var cancellationToken = (CancellationToken)obj;

                    using (IModel channel = Connection.CreateModel())
                    {
                        channel.BasicQos(0, 1, false);

                        var consumer = new QueueingBasicConsumer(channel);

                        while (!cancellationToken.IsCancellationRequested)
                        {
                            channel.BasicConsume(workQueue, autoAck, consumer);

                            DequeueMessage(consumer, channel);
                        }
                    }
                }, token, token, TaskCreationOptions.None);

            runningConsumers.Add(task);

            task.ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        Configure.Instance.OnCriticalError("Failed to start consumer.", t.Exception);
                    }
                });

            task.Start(scheduler);
        }

        void DequeueMessage(QueueingBasicConsumer consumer, IModel channel)
        {
            object rawMessage;

            if (!consumer.Queue.Dequeue(1000, out rawMessage))
                return;

            var message = (BasicDeliverEventArgs)rawMessage;

            //todo - add dead lettering
            bool messageProcessedOk = TryProcessMessage(RabbitMqTransportMessageExtensions.ToTransportMessage(message));

            if (!autoAck && messageProcessedOk)
                channel.BasicAck(message.DeliveryTag, false);
        }


        void Purge()
        {
            using (var channel = Connection.CreateModel())
            {
                channel.QueuePurge(workQueue);
            }
        }

        Func<TransportMessage, bool> TryProcessMessage;
        readonly List<Task> runningConsumers = new List<Task>();
        bool autoAck;
        MTATaskScheduler scheduler;
        readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
        string workQueue;
    }
}