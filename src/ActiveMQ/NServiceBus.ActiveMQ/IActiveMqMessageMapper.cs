namespace NServiceBus.Transport.ActiveMQ
{
    using Apache.NMS;

    public interface IActiveMqMessageMapper
    {
        IMessage CreateJmsMessage(TransportMessage message, ISession session);

        TransportMessage CreateTransportMessage(IMessage message);
    }
}