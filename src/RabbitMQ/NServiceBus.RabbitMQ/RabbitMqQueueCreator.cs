namespace NServiceBus.Transport.RabbitMQ
{
    using NServiceBus.Config;
    using Unicast.Queuing;
    using global::RabbitMQ.Client;

    public class RabbitMqQueueCreator : ICreateQueues
    {

        public IConnection Connection { get; set; }

        public void CreateQueueIfNecessary(Address address, string account)
        {
            var durable = !Endpoint.IsVolatile;

            using (var channel = Connection.CreateModel())
            {
                channel.QueueDeclare(address.Queue, durable, false, false, null);
                channel.ExchangeDeclare(address.Queue + ".events","topic",durable);
            }
                
        }
    }
}