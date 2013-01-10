namespace NServiceBus.Transport.ActiveMQ
{
    using Apache.NMS;

    public interface IActiveMqMessageEncoder
    {
        IMessage Encode(TransportMessage message, ISession session);
    }
}