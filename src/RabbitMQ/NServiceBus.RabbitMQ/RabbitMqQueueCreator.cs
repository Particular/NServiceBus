namespace NServiceBus.RabbitMq
{
    using NServiceBus.Unicast.Queuing;
    using global::RabbitMQ.Client;

    public class RabbitMqQueueCreator : ICreateQueues
    {

        public IConnection Connection { get; set; }

        public void CreateQueueIfNecessary(Address address, string account)
        {
            var durable = Configure.Endpoint.Advanced().DurableMessages;

            using (var channel = Connection.CreateModel())
            {
                channel.QueueDeclare(address.Queue, durable, false, false, null);

                //only setup a exchange for the main endpoint queue
                if (address == Address.Local)
                {
                    channel.ExchangeDeclare(address.Queue + ".events", "topic", durable);
                }
            }

        }
    }
}