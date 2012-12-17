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

        public void Init(Address address, TransactionSettings transactionSettings, Func<bool> commitTransation)
        {
            workQueue = address.Queue;
            autoAck = !transactionSettings.IsTransactional;
            shouldCommitTransation = commitTransation;
        }

        public void Start(int maximumConcurrencyLevel)
        {
            //todo do we need a custom scheduler ?
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

                        var channelAborted = false;

                        while (!channelAborted && !cancelationToken.IsCancellationRequested)
                        {
                            channel.BasicConsume(workQueue, autoAck, consumer);

                            channelAborted = DequeueMessage(consumer, channel);
                        }
                    }

                }, cancelationToken, TaskCreationOptions.None, scheduler)
            .ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    Logger.Error("Error processing message.", t.Exception.GetBaseException());
                }

                Configure.Instance.OnCriticalError("Failed to start consumer", t.Exception);
            });

            runningConsumers.Add(new RunningConsumer
                {
                    CancellationTokenSource = cancellationTokenSource,
                    Task = task
                });
        }

        bool DequeueMessage(QueueingBasicConsumer consumer, IModel channel)
        {
            try
            {
                object rawMessage;

                if (!consumer.Queue.Dequeue(1000, out rawMessage))
                    return false;

                var message = (BasicDeliverEventArgs)rawMessage;

                MessageDequeued(this, new TransportMessageAvailableEventArgs(message.ToTransportMessage()));

                if (!autoAck && shouldCommitTransation())
                    channel.BasicAck(message.DeliveryTag, false);
            }
            catch (OperationInterruptedException ex)
            {
                // The consumer was removed, either through
                // channel or connection closure, or through the
                // action of IModel.BasicCancel().
                Logger.Warn("The channel was aborted with the foloowing ex", ex);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error processing message.", ex.GetBaseException());

                //todo refactor the circuit breaker from MSMQ out and connect it to a call from here so that we can shutdown the 
                // endpoint on to many recurring failures
                return false;
            }

            return false;
        }



        public void Stop()
        {
            //tell all tasks to cancel
            runningConsumers.ForEach(consumer => consumer.CancellationTokenSource.Cancel());


            //and give them a few seconds to wrap up their work
            Task.WaitAll(runningConsumers.Select(c => c.Task).ToArray(), TimeSpan.FromSeconds(3));
        }


        public event EventHandler<TransportMessageAvailableEventArgs> MessageDequeued;

        public IConnection Connection { get; set; }

        readonly List<RunningConsumer> runningConsumers = new List<RunningConsumer>();
        bool autoAck;

        string workQueue;
        Func<bool> shouldCommitTransation;
        static readonly ILog Logger = LogManager.GetLogger("Transport");
        TaskScheduler scheduler;

        class RunningConsumer
        {
            public CancellationTokenSource CancellationTokenSource { get; set; }
            public Task Task { get; set; }
        }
    }
}