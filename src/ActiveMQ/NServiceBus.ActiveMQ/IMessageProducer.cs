namespace NServiceBus.Transport.ActiveMQ
{
    public interface IMessageProducer
    {
        void SendMessage(TransportMessage message, string destination, string destinationPrefix);
    }
}