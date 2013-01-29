namespace NServiceBus.RabbitMq
{
    using Unicast.Queuing;

    public class RabbitMqMessageSender : ISendMessages
    {
        public void Send(TransportMessage message, Address address)
        {
            UnitOfWork.Add(channel =>
                {
                    var properties = RabbitMqTransportMessageExtensions.FillRabbitMqProperties(message,channel.CreateBasicProperties());

                    channel.BasicPublish(string.Empty, address.Queue, true, false, properties, message.Body);
                });
        }

        public RabbitMqUnitOfWork UnitOfWork { get; set; }
    }
}