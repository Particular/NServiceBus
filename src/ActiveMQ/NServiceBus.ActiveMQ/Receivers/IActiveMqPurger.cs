namespace NServiceBus.Transports.ActiveMQ.Receivers
{
    using Apache.NMS;

    public interface IActiveMqPurger
    {
        void Purge(ISession session, IDestination destination);
    }
}