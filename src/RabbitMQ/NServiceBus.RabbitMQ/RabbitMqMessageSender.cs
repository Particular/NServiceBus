namespace NServiceBus.RabbitMQ
{
    using Unicast.Queuing;
    using global::RabbitMQ.Client;
    using global::RabbitMQ.Client.Impl;

    public class RabbitMqMessageSender : ISendMessages
    {
        public IConnection Connection { get; set; }

        public void Send(TransportMessage message, Address address)
        {

            using (var channel = Connection.CreateModel())
            {
                var properties = message.RabbitMqProperties(channel);

                channel.BasicPublish("", address.Queue, true, false, properties, message.Body);
            }
        }

      
    }

    public static class RabbitMQTransportMessageExtensions
    {
        public static IBasicProperties RabbitMqProperties(this TransportMessage message,IModel channel)
        {
            
            var properties = channel.CreateBasicProperties();

            //props.ContentType = "text/plain";
            properties.ReplyTo = message.ReplyToAddress.Queue;
            properties.DeliveryMode = 2;

            return properties;
        }
    }
}