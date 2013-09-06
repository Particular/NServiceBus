namespace NServiceBus.Transports.ActiveMQ
{
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
