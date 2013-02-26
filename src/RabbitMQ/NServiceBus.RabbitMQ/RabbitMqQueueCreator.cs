namespace NServiceBus.Transports.RabbitMQ
{
    using NServiceBus.Unicast.Queuing;

    public class RabbitMqQueueCreator : ICreateQueues
    {
        public IManageRabbitMqConnections ConnectionManager { get; set; }

        public void CreateQueueIfNecessary(Address address, string account)
        {
            var durable = Configure.Endpoint.Advanced().DurableMessages;

            using (var channel = ConnectionManager.GetConnection(ConnectionPurpose.Administration).CreateModel())
            {
                channel.QueueDeclare(address.Queue, durable, false, false, null);
            }

        }
    }
}