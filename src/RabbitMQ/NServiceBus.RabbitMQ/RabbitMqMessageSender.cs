namespace NServiceBus.RabbitMq
{
    using System;
    using System.Transactions;
    using Unicast.Queuing;
    using global::RabbitMQ.Client;

    public class RabbitMqMessageSender : ISendMessages
    {
        public IConnection Connection { get; set; }


        public void Send(TransportMessage message, Address address)
        {
            if (Transaction.Current == null)
            {
                PublishToRabbit(message, address);
            }
            else
            {
                if (resourceManager == null)
                {
                    resourceManager = new RabbitMqSendResourceManager();

                    Transaction.Current.EnlistVolatile(resourceManager, EnlistmentOptions.None);
                }


                resourceManager.Add(() => PublishToRabbit(message, address));
            }

        }

        void PublishToRabbit(TransportMessage message, Address address)
        {

            using (var channel = Connection.CreateModel())
            {
                var properties = RabbitMqTransportMessageExtensions.FillRabbitMqProperties(message, channel.CreateBasicProperties());

                channel.BasicPublish(string.Empty, address.Queue, true, false, properties, message.Body);
            }
        }

        [ThreadStatic]
        static RabbitMqSendResourceManager resourceManager;
    }
}