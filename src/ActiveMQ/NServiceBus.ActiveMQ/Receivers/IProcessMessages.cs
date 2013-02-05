namespace NServiceBus.Transport.ActiveMQ.Receivers
{
    using Apache.NMS;

    public interface IProcessMessages
    {
        void ProcessMessage(IMessage message);
    }
}