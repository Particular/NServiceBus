namespace NServiceBus.RabbitMQ
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Schedulers;
    using Logging;
    using Unicast.Transport.Transactional;
    using global::RabbitMQ.Client;
    using global::RabbitMQ.Client.Events;
    using global::RabbitMQ.Client.Exceptions;

    public class RabbitMqDequeueStrategy : IDequeueMessages
    {

        public void Init(Address address, TransactionSettings transactionSettings)
        {
            workQueue = address.Queue;
            autoAck = !transactionSettings.IsTransactional;
        }

        public void Start(int maximumConcurrencyLevel)
        {
            scheduler = new MTATaskScheduler(maximumConcurrencyLevel, String.Format("NServiceBus Dequeuer Worker Thread for [{0}]", workQueue));

            for (int i = 0; i < maximumConcurrencyLevel; i++)
            {
                StartConsumer();
            }
        }

        void StartConsumer()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var cancelationToken = cancellationTokenSource.Token;

            var task = Task.Factory.StartNew(() =>
                {
                    using (var channel = Connection.CreateModel())
                    {
                        channel.BasicQos(0, 1, false);

                        var consumer = new QueueingBasicConsumer(channel);

                        
                        while (!cancelationToken.IsCancellationRequested)
                        {
                            channel.BasicConsume(workQueue, autoAck, consumer);

                            DequeueMessage(consumer, channel);
                        }
                    }

                }, cancelationToken, TaskCreationOptions.LongRunning, scheduler)
            .ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    Configure.Instance.OnCriticalError("Failed to start consumer", t.Exception);
                }
            });

            runningConsumers.Add(new RunningConsumer
                {
                    CancellationTokenSource = cancellationTokenSource,
                    Task = task
                });
        }

        void DequeueMessage(QueueingBasicConsumer consumer, IModel channel)
        {
            object rawMessage;

            if (!consumer.Queue.Dequeue(1000, out rawMessage))
                return;

            var message = (BasicDeliverEventArgs)rawMessage;

            //todo - add dead lettering
            var messageProcessedOk = TryProcessMessage(message.ToTransportMessage());

            if (!autoAck && messageProcessedOk)
                channel.BasicAck(message.DeliveryTag, false);
        }



        public void Stop()
        {
            //tell all tasks to cancel
            runningConsumers.ForEach(consumer => consumer.CancellationTokenSource.Cancel());


            //and give them a few seconds to wrap up their work
            Task.WaitAll(runningConsumers.Select(c => c.Task).ToArray(), TimeSpan.FromSeconds(3));
        }

        public Func<TransportMessage, bool> TryProcessMessage { get; set; }


        public IConnection Connection { get; set; }

        readonly List<RunningConsumer> runningConsumers = new List<RunningConsumer>();
        bool autoAck;

        string workQueue;
        TaskScheduler scheduler;

        class RunningConsumer
        {
            public CancellationTokenSource CancellationTokenSource { get; set; }
            public Task Task { get; set; }
        }
    }
}