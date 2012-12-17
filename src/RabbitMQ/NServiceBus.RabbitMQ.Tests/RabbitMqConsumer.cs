namespace NServiceBus.RabbitMQ.Tests
{
    using System;
    using global::RabbitMQ.Client;
    using global::RabbitMQ.Client.Events;

    public class RabbitMqConsumer
    {
        public void Start()
        {
            using (var channel = Connection.CreateModel())
            {
                var consumer = new QueueingBasicConsumer(channel);

                while (true)
                {
                    channel.BasicConsume(QueueName, true, consumer);

                    var message = (BasicDeliverEventArgs)consumer.Queue.Dequeue();              
                }
            }


        }

        public string QueueName { get; set; }

        public IConnection Connection { get; set; }
    }
}