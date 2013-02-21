namespace NServiceBus.RabbitMq
{
    using System;
    using Unicast.Queuing;

    public class RabbitMqQueueCreator : ICreateQueues
    {
        public IManageRabbitMqConnections ConnectionManager { get; set; }

        public Func<Address, string> ExchangeName { get; set; }

        public void CreateQueueIfNecessary(Address address, string account)
        {
            var durable = Configure.Endpoint.Advanced().DurableMessages;

            using (var channel = ConnectionManager.GetConnection(ConnectionPurpose.Administration, "create_queue").CreateModel())
            {
                channel.QueueDeclare(address.Queue, durable, false, false, null);
            }

        }
    }
}