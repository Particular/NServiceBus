namespace NServiceBus.Transports.ActiveMQ
{
    using Apache.NMS;

    public interface IActiveMqMessageEncoderPipeline
    {
        IMessage Encode(TransportMessage message, ISession session);
    }
}