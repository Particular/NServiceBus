namespace NServiceBus.RabbitMQ
{
    using Unicast.Queuing;
    using global::RabbitMQ.Client;

    public class RabbitMqMessageSender : ISendMessages
    {
        public IConnection Connection { get; set; }

        public void Send(TransportMessage message, Address address)
        {

            using (var channel = Connection.CreateModel())
            {  
                var properties = message.FillRabbitMqProperties(channel.CreateBasicProperties());

                channel.BasicPublish("", address.Queue, true, false, properties, message.Body);

            }
        }


    }
}