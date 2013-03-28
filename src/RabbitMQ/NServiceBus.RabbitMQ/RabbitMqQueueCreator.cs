namespace NServiceBus.Transports.RabbitMQ
{
    using Settings;

    public class RabbitMqQueueCreator : ICreateQueues
    {
        public IManageRabbitMqConnections ConnectionManager { get; set; }

        public void CreateQueueIfNecessary(Address address, string account)
        {
            var durable = SettingsHolder.Get<bool>("Endpoint.DurableMessages");

            using (var channel = ConnectionManager.GetConnection(ConnectionPurpose.Administration).CreateModel())
            {
                channel.QueueDeclare(address.Name, durable, false, false, null);
            }

        }
    }
}