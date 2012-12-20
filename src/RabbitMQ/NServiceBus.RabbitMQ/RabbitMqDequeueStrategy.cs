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
        private readonly List<Task> runningConsumers = new List<Task>();
        private bool autoAck;
        private MTATaskScheduler scheduler;
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
        private string workQueue;

        public IConnection Connection { get; set; }

        /// <summary>
        /// Initialises the <see cref="IDequeueMessages"/>.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="transactionSettings">The <see cref="TransactionSettings"/> to be used by <see cref="IDequeueMessages"/>.</param>
        public void Init(Address address, TransactionSettings transactionSettings)
        {
            workQueue = address.Queue;
            autoAck = !transactionSettings.IsTransactional;


        }

        /// <summary>
        /// Starts the dequeuing of message using the specified <paramref name="maximumConcurrencyLevel"/>.
        /// </summary>
        /// <param name="maximumConcurrencyLevel">Indicates the maximum concurrency level this <see cref="IDequeueMessages"/> is able to support.</param>
        public void Start(int maximumConcurrencyLevel)
        {
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

        /// <summary>
        /// Called when a message has been dequeued and is ready for processing.
        /// </summary>
        public Func<TransportMessage, bool> TryProcessMessage { get; set; }

        private void StartConsumer()
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

        private void DequeueMessage(QueueingBasicConsumer consumer, IModel channel)
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
    }
}