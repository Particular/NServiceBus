namespace NServiceBus.RabbitMQ
{
    using Unicast.Queuing;
    using global::RabbitMQ.Client;

    public class RabbitMqMessageSender:ISendMessages
    {
        public IConnection Connection { get; set; }
     
        public void Send(TransportMessage message, Address address)
        {
            using (IModel channel = Connection.CreateModel())
            {
                channel.BasicPublish("", address.Queue, null, message.Body);
            }
        }
    }
}