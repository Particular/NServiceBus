namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    using Apache.NMS;

    public interface IActiveMqMessageMapper
    {
        IMessage CreateJmsMessage(TransportMessage message, INetTxSession session);

        TransportMessage CreateTransportMessage(IMessage message);
    }
}