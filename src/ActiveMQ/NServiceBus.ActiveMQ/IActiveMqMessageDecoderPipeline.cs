namespace NServiceBus.Transports.ActiveMQ
{
    using Apache.NMS;

    public interface IActiveMqMessageDecoderPipeline
    {
        void Decode(TransportMessage transportMessage, IMessage message);
    }
}