namespace NServiceBus.Transport.ActiveMQ
{
    using Apache.NMS;

    public interface IActiveMqPurger
    {
        void Purge(ISession session, IDestination destination);
    }
}