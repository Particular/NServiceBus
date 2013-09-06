namespace NServiceBus.Transports.ActiveMQ
{
    using NServiceBus.Unicast.Queuing;

    public class ActiveMqMessageSender : ISendMessages
    {
        private readonly IMessageProducer messageProducer;

        public ActiveMqMessageSender(IMessageProducer messageProducer)
        {
            this.messageProducer = messageProducer;
        }

        public void Send(TransportMessage message, Address address)
        {
            messageProducer.SendMessage(message, address.Queue, "queue://");
        }
    }
}
